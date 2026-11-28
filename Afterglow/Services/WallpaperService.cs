using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Afterglow.Core;
using Afterglow.PlatformInterop;
using Avalonia.Threading;
using PowerKit.Extensions;
using SkiaSharp;

namespace Afterglow.Services;

/// <summary>
/// What the dynamic wallpaper follows.
/// </summary>
/// <param name="Configuration">The screen's color temperature and brightness (for the accent color).</param>
/// <param name="SunElevation">How high the sun is, in degrees (negative below the horizon).</param>
/// <param name="IsImmediate">Whether it's shown right away (when stepping through the day).</param>
public readonly record struct WallpaperTarget(
    ColorConfiguration Configuration,
    double SunElevation,
    bool IsImmediate = false
);

/// <summary>
/// Sets the desktop wallpaper to a sky for where the sun is (see <see cref="DynamicWallpaper" />),
/// and the Windows accent color to the light's color and brightness, for Windows Dynamic
/// Lighting.
/// </summary>
/// <remarks>
/// <para>
/// The images are natural skies, which only depend on how high the sun is: the screen's tint
/// warms and dims them live, like everything else on the screen, and the accent color carries
/// Afterglow's color and brightness. So a fixed set of skies (one for each of the stops along the
/// sun's path, shared by the morning and the evening) is drawn once, for each display size: the
/// one that's needed first right away, and the others one at a time in the background. After
/// that, following the sun is just pointing Windows at another image.
/// </para>
/// <para>
/// Following the sun and the cycle waits for a quiet moment, so that nothing changes in the
/// middle of something:
/// <list type="bullet">
/// <item>A display that's covered by a fullscreen application, or that the user is working on
/// (where the mouse or the window in focus is, while there's been input in the last few seconds),
/// keeps its sky until it isn't. The other displays carry on.</item>
/// <item>The accent color waits until the user is away: no input (from the mouse, the keyboard,
/// or a game controller) for half a minute, nothing fullscreen, nothing keeping the display on (a
/// video or a game playing, wherever it is: Windows doesn't say which display), and nothing moving
/// on the screen (the GPU barely busy, so that a game in a window, or a video that's just being
/// watched, counts too), or the screen locked. Windows has every application re-theme at once when it changes, which stalls them all
/// (and, through the applications that watch the mouse, the mouse too) for up to a couple of
/// seconds, so it's never done while someone might notice. It also only changes once it looks
/// clearly different (about a handful of times over a transition, and not at all in between),
/// rather than following every small step. The only exceptions are turning it on and stepping
/// through the day, which show it right away.</item>
/// <item>Changing the wallpaper itself costs next to nothing (measured at 2 to 6 ms, without
/// stalling anything), so it doesn't wait for any of that.</item>
/// <item>The wallpaper doesn't change while the screen is locked, and nothing is drawn in the
/// background while an application is fullscreen.</item>
/// </list>
/// Stepping through the day (by hand, to see how it all looks) shows the sky and the accent color
/// right away, whatever it costs, and stopping it puts the accent color back right away.
/// </para>
/// <para>
/// Images are drawn at each display's own resolution (so Windows doesn't scale them), dithered
/// (so gradients don't show bands), and saved as PNG (as Windows compresses JPEG wallpapers again,
/// which brings the bands back).
/// </para>
/// </remarks>
public class WallpaperService : IDisposable
{
    // Checked often while something is waiting (or the user just changed something), and
    // rarely otherwise (the sun moves slowly), so that it wakes up as little as possible
    private static readonly TimeSpan FastCheckInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan SlowCheckInterval = TimeSpan.FromSeconds(15);

    // How long checks stay frequent after the user changes something
    private static readonly TimeSpan FastCheckTime = TimeSpan.FromSeconds(30);

    // How often the accent color follows the cycle's slow drift, at most
    private static readonly TimeSpan AccentDriftInterval = TimeSpan.FromMinutes(2);

    // How long without mouse or keyboard input a display counts as not being worked on
    private static readonly TimeSpan QuietTime = TimeSpan.FromSeconds(10);

    // How long without any input (including game controllers) counts as the user being away
    private static readonly TimeSpan AwayTime = TimeSpan.FromSeconds(30);

    // Game controllers and the GPU are only looked at during checks, so what happened since the
    // previous one is only known if that one was recent (otherwise, the time starts over)
    private static readonly TimeSpan ActivityCheckGap = SlowCheckInterval * 2;

    // How busy the GPU can be (drawing and playing video, in percent, on average since the
    // previous check) with nothing moving on the screen: a still desktop measured at 2 to 4%,
    // while a game (even in a window, or in its menus) or a video keeps it well above this
    private const double BusyGpuUsage = 10;

    // How long the background drawing waits while a video or a game is playing
    private static readonly TimeSpan BusyRetryDelay = TimeSpan.FromSeconds(30);

    // Smaller changes of the accent color aren't worth it (see the remarks): about twice the
    // smallest difference that can be seen side by side, which splits a typical transition from
    // day to night into some 5 to 15 steps
    private const double NoticeableAccentDifference = 0.04;

    // Images drawn by an earlier version (e.g. with other skies) are told apart by this
    private const string ImagePrefix = "Sky 4 ";

    private const string OriginalWallpapersFileName = "Original wallpapers.txt";
    private const string OriginalAccentColorFileName = "Original accent color.txt";

    // The accent color this set last (to tell whether the user has chosen another one since)
    private const string AppliedAccentColorFileName = "Applied accent color.txt";

    private readonly SettingsService _settingsService;
    private readonly ExternalApplicationService _externalApplicationService;
    private readonly string _dirPath;
    private readonly IDisposable _settingsSubscription;
    private IDisposable? _sessionSubscription;

    // All work with the files, the wallpaper and the accent color happens one piece at a time
    private readonly SemaphoreSlim _lock = new(1, 1);

    private Func<WallpaperTarget?>? _getTarget;
    private DispatcherTimer? _timer;
    private bool _isStarted;
    private bool _isBusy;

    // What the user is doing (see UpdateActivity)
    private Rect? _fullScreenBounds;
    private IReadOnlyList<Rect> _busyBounds = [];
    private bool _isLocked;
    private bool _isDisplayKeptOn;
    private bool _isAway;
    private volatile bool _isDrawingPaused;
    private DateTimeOffset _activityCheckedAt = DateTimeOffset.MinValue;
    private int? _controllerState;
    private DateTimeOffset _controllersQuietSince = DateTimeOffset.MaxValue;
    private GpuUsage _gpuUsage = new();
    private Task<(double? Usage, bool IsContinuous)> _gpuSample = Task.FromResult<(double?, bool)>(
        (null, false)
    );
    private DateTimeOffset _gpuSampledAt = DateTimeOffset.MinValue;

    // What's shown now (null until shown on every display), and a sky only shown on the displays
    // that weren't busy (along with which ones were)
    private int? _shownStop;
    private (int Stop, string Skipped)? _partialStop;
    private WallpaperColor? _appliedAccentColor;
    private WallpaperColor? _lastAccentTarget;
    private DateTimeOffset _lastAccentAppliedAt = DateTimeOffset.MinValue;

    // Whether the accent color was just turned on (so it's shown right away, once), and whether it
    // was on before (settings are re-announced on every save, so a change is told apart this way)
    private bool _isAccentRequested;
    private bool _wasImmediate;
    private bool _wasAccentColorMatched;

    // The display sizes that images are drawn for, and the drawing of the rest in the background
    private IReadOnlyList<(int Width, int Height)> _sizes = [];
    private CancellationTokenSource? _drawingCancellation;
    private Task _drawing = Task.CompletedTask;

    public WallpaperService(
        SettingsService settingsService,
        ExternalApplicationService externalApplicationService
    )
    {
        _settingsService = settingsService;
        _externalApplicationService = externalApplicationService;
        _dirPath = GetDirPath();

        _settingsSubscription = PowerKit.Disposable.Merge(
            settingsService.WatchProperties(
                [o => o.IsDynamicWallpaperEnabled, o => o.IsAccentColorMatched],
                Evaluate
            ),
            // The sky is checked again right away (the accent color follows once the user is away)
            settingsService.WatchProperties(
                [o => o.LightWarmth, o => o.LightDimming],
                NotifyTargetChanged
            )
        );
    }

    // Next to the settings
    private static string GetDirPath() =>
        Path.Combine(
            Path.GetDirectoryName(StartOptions.Current.SettingsPath) ?? AppContext.BaseDirectory,
            "Wallpapers"
        );

    public void SetTargetSource(Func<WallpaperTarget?> getTarget) => _getTarget = getTarget;

    public void Start()
    {
        _isStarted = true;

        // Locking the screen (or waking up to the lock screen) is the best moment for the accent
        // color, as nobody's there to notice, so it's checked right away rather than at the
        // regular pace (which could miss a quick unlock)
        _sessionSubscription = PowerKit.Disposable.Merge(
            SessionEvents.RegisterLock(isLocked =>
            {
                if (isLocked)
                    NotifyTargetChanged();
            }),
            SessionEvents.RegisterSleep(isSuspending =>
            {
                if (!isSuspending)
                    NotifyTargetChanged();
            })
        );

        // (already being on at startup isn't turning it on, so the accent color only changes once
        // the user is away, and only if it isn't still right from before)
        _wasAccentColorMatched = IsAccentColorMatched;
        _appliedAccentColor = TryGetStillAppliedAccentColor();
        Evaluate();
    }

    private bool IsEnabled => _isStarted && _settingsService.IsDynamicWallpaperEnabled;

    private bool IsAccentColorMatched => IsEnabled && _settingsService.IsAccentColorMatched;

    private void Evaluate()
    {
        if (!_isStarted)
            return;

        if (IsEnabled)
        {
            if (_timer is null)
            {
                _timer = new DispatcherTimer(DispatcherPriority.Background)
                {
                    Interval = FastCheckInterval,
                };
                _timer.Tick += (_, _) => Check();
                _timer.Start();
            }

            // The accent color was turned off on its own
            if (!IsAccentColorMatched && _appliedAccentColor is not null)
            {
                _appliedAccentColor = null;
                _ = RunLockedAsync(() => RestoreAccentColor(_dirPath));
            }

            // Turned on just now (by the user): shown right away, so that it can be seen working
            if (IsAccentColorMatched && !_wasAccentColorMatched)
                _isAccentRequested = true;
            else if (!IsAccentColorMatched)
                _isAccentRequested = false;
            _wasAccentColorMatched = IsAccentColorMatched;

            Check();
        }
        else
        {
            _timer?.Stop();
            _timer = null;
            _drawingCancellation?.Cancel();
            _drawingCancellation = null;
            _shownStop = null;
            _partialStop = null;
            _appliedAccentColor = null;
            _lastAccentTarget = null;
            _isAccentRequested = false;
            _wasAccentColorMatched = false;
            _gpuUsage.Dispose();
            _gpuUsage = new GpuUsage();
            _gpuSample = Task.FromResult<(double?, bool)>((null, false));

            // Also puts back what was left over from an earlier run (e.g. after a crash)
            _ = RunLockedAsync(() =>
            {
                RestoreAccentColor(_dirPath);
                RestoreWallpapers(_dirPath);
            });
        }
    }

    private async Task RunLockedAsync(Action action)
    {
        await _lock.WaitAsync();
        try
        {
            await Task.Run(action);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Dynamic wallpaper: {ex}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Checks whether the wallpaper needs to change, without waiting for the next regular check.
    /// </summary>
    public void NotifyTargetChanged()
    {
        _fastCheckUntil = DateTimeOffset.Now + FastCheckTime;
        Check();
    }

    private DateTimeOffset _fastCheckUntil = DateTimeOffset.MinValue;

    private void SetCheckInterval(bool isFast)
    {
        var interval =
            isFast || DateTimeOffset.Now < _fastCheckUntil ? FastCheckInterval : SlowCheckInterval;

        if (_timer is not null && _timer.Interval != interval)
            _timer.Interval = interval;
    }

    // Only looked up when there's something to do, as it opens the foreground application
    private void UpdateActivity()
    {
        // The cheapest first: while the screen is locked, the user is away and the wallpaper
        // doesn't change, so the foreground application isn't looked up (and it's the best time
        // to draw)
        _isLocked = InputDesktop.IsLocked();
        if (_isLocked)
        {
            _isAway = true;
            _isDrawingPaused = false;
            return;
        }

        _isDisplayKeptOn = UserActivity.IsDisplayKeptOn();

        // (a system window, like the taskbar, keeps what was found before)
        if (_externalApplicationService.TryGetForegroundFullScreenMonitor(out var deviceName))
        {
            _fullScreenBounds = deviceName is null
                ? null
                : Afterglow
                    .PlatformInterop.Monitor.GetAll()
                    .FirstOrDefault(m =>
                        string.Equals(
                            m.TryGetDeviceName(),
                            deviceName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    ?.TryGetBounds();
        }

        var idleTime = UserActivity.GetIdleTime();
        var isIdle = idleTime >= QuietTime;

        // The displays to leave alone for now: one that's covered, and the ones being worked on
        var busyBounds = new List<Rect>();
        if (_fullScreenBounds is { } fullScreenBounds)
            busyBounds.Add(fullScreenBounds);
        if (!isIdle)
        {
            foreach (var bounds in UserActivity.GetActiveMonitorBounds())
            {
                if (!busyBounds.Contains(bounds))
                    busyBounds.Add(bounds);
            }
        }

        _busyBounds = busyBounds;
        // Changing the accent color stalls every application for a moment (see the remarks), so
        // it waits for the user to be away. Changing the wallpaper costs next to nothing, so the
        // displays that aren't busy carry on meanwhile. Drawing (at the lowest priority) only
        // waits for a fullscreen application.
        var isStill = UpdateStillness();

        _isAway = idleTime >= AwayTime && isStill && _fullScreenBounds is null && !_isDisplayKeptOn;
        _isDrawingPaused = _fullScreenBounds is not null;
    }

    // Whether nothing has happened that Windows doesn't count as input: no game controller used
    // for as long as it takes to be away, and nothing on the screen moving (a game in a window,
    // or a video that doesn't keep the display on) since the previous check. Both are only known
    // when checked regularly, so anything else counts as not still.
    private bool UpdateStillness()
    {
        var now = DateTimeOffset.Now;
        var isContinuous = now - _activityCheckedAt <= ActivityCheckGap;
        _activityCheckedAt = now;

        var controllerState = UserActivity.TryGetControllerState();
        if (controllerState != _controllerState || !isContinuous)
            _controllersQuietSince = now;
        _controllerState = controllerState;

        // The GPU is read in the background, at the lowest priority (starting to measure it takes
        // a moment, longer on slower computers), so what's known here is the reading that the
        // previous check started: how busy it was between the two checks before. If it can't be
        // measured at all, the other signs have to do.
        var isGpuIdle =
            _gpuSample.IsCompletedSuccessfully
            && (
                !_gpuUsage.IsAvailable
                || (_gpuSample.Result.IsContinuous && _gpuSample.Result.Usage < BusyGpuUsage)
            );

        if (_gpuSample.IsCompleted)
        {
            var gpuUsage = _gpuUsage;
            var isSampleContinuous = now - _gpuSampledAt <= ActivityCheckGap;
            _gpuSampledAt = now;
            _gpuSample = Task.Run(() =>
            {
                using var backgroundMode = BackgroundWork.EnterBackgroundMode();
                return (gpuUsage.TryGetUsage(), isSampleContinuous);
            });
        }

        return now - _controllersQuietSince >= AwayTime && isGpuIdle;
    }

    private static string GetKey(IReadOnlyList<Rect> bounds) =>
        string.Join(';', bounds.Select(b => $"{b.Left},{b.Top},{b.Right},{b.Bottom}").Order());

    private void Check()
    {
        if (!IsEnabled || _isBusy || _getTarget?.Invoke() is not { } target)
            return;

        var stop = DynamicWallpaper.GetStopIndex(target.SunElevation);
        var accent = DynamicWallpaper.GetAccentColor(
            target.Configuration.Temperature,
            target.Configuration.Brightness,
            _settingsService.LightWarmth,
            _settingsService.LightDimming
        );

        // The accent color is still moving (e.g. a slider being dragged, or the day being stepped
        // through), as opposed to the cycle's slow drift
        var isAccentSettled =
            _lastAccentTarget is not { } lastAccent
            || WallpaperColor.GetDifference(lastAccent, accent) <= 1;
        _lastAccentTarget = accent;

        var isSkyNeeded = _shownStop != stop;

        // Stepping through the day is for seeing how it all looks, so the accent color is shown
        // right away along with everything else (in the same steps as it would be otherwise),
        // whatever it costs, and it goes back to the time of day right away once that stops
        if (_wasImmediate && !target.IsImmediate)
            _isAccentRequested = true;
        _wasImmediate = target.IsImmediate;
        var isAccentForced = _isAccentRequested || target.IsImmediate;

        var accentDifference = _appliedAccentColor is { } applied
            ? WallpaperColor.GetPerceptualDifference(applied, accent)
            : double.MaxValue;
        var isAccentNeeded =
            IsAccentColorMatched
            && accentDifference >= NoticeableAccentDifference
            && (
                isAccentForced
                || (
                    isAccentSettled
                    && DateTimeOffset.Now - _lastAccentAppliedAt >= AccentDriftInterval
                )
            );

        // Checked often while anything is waiting, or still moving. An accent color waiting for
        // the user to be away is checked at the regular pace (being away takes minutes anyway).
        var isWaiting =
            isSkyNeeded || !_drawing.IsCompleted || !isAccentSettled || _isAccentRequested;
        SetCheckInterval(isWaiting);

        // Also keeps the background drawing informed
        if (!isSkyNeeded && !isAccentNeeded && _drawing.IsCompleted)
            return;

        UpdateActivity();

        // The accent color waits for the user to be away (unless it's forced, see above)
        if (isAccentNeeded && !isAccentForced && !_isAway)
            isAccentNeeded = false;

        IReadOnlyList<Rect> skipped = [];
        if (!target.IsImmediate)
        {
            // (while the screen is locked, only the accent color changes)
            if (_isLocked)
                isSkyNeeded = false;

            skipped = _busyBounds;
        }

        // (a sky already shown on every display that isn't busy is only shown again on the rest
        // once they aren't)
        var skippedKey = GetKey(skipped);
        if (isSkyNeeded && _partialStop is { } partial && partial.Stop == stop)
            isSkyNeeded = partial.Skipped != skippedKey;

        if (isSkyNeeded || isAccentNeeded)
        {
            Apply(isSkyNeeded ? stop : null, isAccentNeeded ? accent : null, skipped, skippedKey);
        }
    }

    private async void Apply(
        int? stop,
        WallpaperColor? accent,
        IReadOnlyList<Rect> skippedBounds,
        string skippedKey
    )
    {
        _isBusy = true;
        var (isSkyApplied, isSkipped, isAccentApplied) = (false, false, false);

        try
        {
            await _lock.WaitAsync();
            try
            {
                (isSkyApplied, isSkipped, isAccentApplied) = await Task.Run(() =>
                    ApplyCore(stop, accent, skippedBounds)
                );
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            // Tried again on the next check
            Debug.WriteLine($"Dynamic wallpaper: {ex}");
        }
        finally
        {
            _isBusy = false;
        }

        // Turned off meanwhile: put back once it's done
        if (!IsEnabled)
        {
            Evaluate();
            return;
        }

        if (stop is not null && isSkyApplied)
        {
            if (isSkipped)
            {
                _partialStop = (stop.Value, skippedKey);
            }
            else
            {
                _shownStop = stop;
                _partialStop = null;
            }

            StartDrawing(stop.Value);
        }

        if (accent is not null)
        {
            // (if it didn't take, it's tried again at the cycle's pace, rather than right away)
            if (isAccentApplied)
                _appliedAccentColor = accent;
            _lastAccentAppliedAt = DateTimeOffset.Now;
            _isAccentRequested = false;
        }
    }

    private string GetImagePath((int Width, int Height) size, int stop) =>
        Path.Combine(_dirPath, $"{ImagePrefix}{size.Width}x{size.Height} {stop:00}.png");

    // Returns whether the sky was shown (and whether a display was left as it was, as it's
    // busy), and whether the accent color took
    private (bool IsSkyApplied, bool IsSkipped, bool IsAccentApplied) ApplyCore(
        int? stop,
        WallpaperColor? accent,
        IReadOnlyList<Rect> skippedBounds
    )
    {
        var isSkyApplied = false;
        var isSkipped = false;

        if (stop is { } index && DesktopWallpaper.TryCreate() is { } desktop)
        {
            using var _ = desktop;
            var monitors = desktop.GetMonitors();
            if (monitors.Count > 0)
            {
                isSkyApplied = ApplySky(desktop, monitors, index, skippedBounds, out isSkipped);
            }
        }

        // (also when the sky couldn't be shown)
        var isAccentApplied = accent is { } color && SetAccentColor(color);

        return (isSkyApplied, isSkipped, isAccentApplied);
    }

    private bool ApplySky(
        DesktopWallpaper desktop,
        IReadOnlyList<(string Id, Rect Bounds)> monitors,
        int index,
        IReadOnlyList<Rect> skippedBounds,
        out bool isSkipped
    )
    {
        isSkipped = false;

        Directory.CreateDirectory(_dirPath);
        RememberOriginalWallpapers(desktop, monitors);
        DeleteOutdatedImages();

        var sizes = new List<(int, int)>();
        foreach (var (id, bounds) in monitors)
        {
            // Drawn at the display's own size, so that Windows doesn't scale it
            var size = (bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
            if (!sizes.Contains(size))
                sizes.Add(size);

            // Covered by a fullscreen application, or being worked on: changed later (unless
            // it shows this sky already)
            if (skippedBounds.Contains(bounds))
            {
                if (
                    !string.Equals(
                        desktop.TryGetWallpaper(id),
                        GetImagePath(size, index),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                    isSkipped = true;

                continue;
            }

            var filePath = GetImagePath(size, index);
            if (!File.Exists(filePath))
            {
                // (at the lowest priority as well, so that it never gets in the way, even
                // if it takes a moment longer)
                using var backgroundMode = BackgroundWork.EnterBackgroundMode();
                Render(
                    DynamicWallpaper.GetPalette(DynamicWallpaper.GetStops()[index]),
                    size.Item1,
                    size.Item2,
                    filePath,
                    1
                );
            }

            if (
                !string.Equals(
                    desktop.TryGetWallpaper(id),
                    filePath,
                    StringComparison.OrdinalIgnoreCase
                )
            )
                desktop.SetWallpaper(id, filePath);
        }

        if (desktop.Position != DesktopWallpaper.FillPosition)
            desktop.Position = DesktopWallpaper.FillPosition;

        _sizes = sizes;

        return true;
    }

    // Draws the images that aren't there yet, one at a time, closest to the shown one first (as
    // they're needed soonest), and waiting while the desktop can't be seen
    private void StartDrawing(int shownStop)
    {
        if (!_drawing.IsCompleted)
            return;

        var sizes = _sizes;
        var missing = sizes
            .SelectMany(size =>
                Enumerable
                    .Range(0, DynamicWallpaper.StopCount)
                    .Select(stop => (Size: size, Stop: stop))
            )
            .Where(i => !File.Exists(GetImagePath(i.Size, i.Stop)))
            .OrderBy(i => Math.Abs(i.Stop - shownStop))
            .ToArray();

        if (missing.Length <= 0)
            return;

        var cancellation = new CancellationTokenSource();
        _drawingCancellation = cancellation;
        var token = cancellation.Token;

        // On a thread of its own, in Windows' background mode (the lowest priority for the
        // processor, the disk and memory), so that it never gets in the way of anything
        _drawing = Task.Factory.StartNew(
            () =>
            {
                using var backgroundMode = BackgroundWork.EnterBackgroundMode();

                foreach (var (size, stop) in missing)
                {
                    while (_isDrawingPaused)
                    {
                        if (token.WaitHandle.WaitOne(BusyRetryDelay))
                            return;
                    }

                    if (token.IsCancellationRequested)
                        return;

                    var filePath = GetImagePath(size, stop);
                    if (File.Exists(filePath))
                        continue;

                    // Drawn without holding anything up (a change of the wallpaper or the accent
                    // color doesn't wait for it), and only put in place while nothing else works
                    // with the files, so that it isn't left behind once this is turned off
                    string? tempFilePath = null;
                    try
                    {
                        tempFilePath = RenderToTempFile(
                            DynamicWallpaper.GetPalette(DynamicWallpaper.GetStops()[stop]),
                            size.Width,
                            size.Height,
                            filePath,
                            1
                        );

                        _lock.Wait(token);
                        try
                        {
                            if (!File.Exists(filePath))
                            {
                                File.Move(tempFilePath, filePath);
                                tempFilePath = null;
                            }
                        }
                        finally
                        {
                            _lock.Release();
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Debug.WriteLine($"Dynamic wallpaper: {ex}");
                    }
                    finally
                    {
                        if (tempFilePath is not null)
                            TryDelete(tempFilePath);
                    }
                }
            },
            token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );

        // (a cancellation is expected)
        _drawing.ContinueWith(
            t => Debug.WriteLineIf(t.IsFaulted, $"Dynamic wallpaper: {t.Exception}"),
            TaskScheduler.Default
        );
    }

    // Images that this version doesn't use (drawn differently, or the older JPEG ones)
    private void DeleteOutdatedImages()
    {
        foreach (var filePath in Directory.EnumerateFiles(_dirPath))
        {
            var fileName = Path.GetFileName(filePath);
            var isImage =
                fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);

            if (isImage && !fileName.StartsWith(ImagePrefix, StringComparison.Ordinal))
                TryDelete(filePath);
            // (left over from drawing that was cut short, e.g. by a crash: an image is only
            // written out once it's drawn, and put in place right after, so a temporary file
            // never stays for long otherwise)
            else if (
                fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                && File.GetLastWriteTimeUtc(filePath) < DateTime.UtcNow - TimeSpan.FromMinutes(10)
            )
                TryDelete(filePath);
        }
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    // Also any sky drawn by another copy of Afterglow (e.g. a portable one), so that it's never
    // remembered as the user's own wallpaper
    private static bool IsOwnFile(string? filePath, string dirPath) =>
        !string.IsNullOrWhiteSpace(filePath)
        && (
            Path.GetFullPath(filePath)
                .StartsWith(
                    Path.TrimEndingDirectorySeparator(Path.GetFullPath(dirPath))
                        + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase
                )
            || (
                Path.GetFileName(filePath).StartsWith("Sky ", StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    Path.GetFileName(Path.GetDirectoryName(filePath)),
                    "Wallpapers",
                    StringComparison.OrdinalIgnoreCase
                )
            )
        );

    // Remembers each display's own wallpaper (an image, or none for a solid color) and how it was
    // fitted, so that it can be put back. Anything that isn't one of these skies was chosen by
    // the user, also while this is on, so it's always the latest one that's remembered. Kept in a
    // file, so that it's also put back if Afterglow is turned off after a restart (or
    // uninstalled).
    private void RememberOriginalWallpapers(
        DesktopWallpaper desktop,
        IReadOnlyList<(string Id, Rect Bounds)> monitors
    )
    {
        var filePath = Path.Combine(_dirPath, OriginalWallpapersFileName);
        var lines = File.Exists(filePath) ? File.ReadAllLines(filePath).ToList() : [];
        var isChanged = !File.Exists(filePath);

        if (!lines.Any(l => l.StartsWith("position\t", StringComparison.Ordinal)))
        {
            lines.Add($"position\t{desktop.Position}");
            isChanged = true;
        }

        foreach (var (id, _) in monitors)
        {
            // (unknown, or one of these skies)
            var wallpaper = desktop.TryGetWallpaper(id);
            if (wallpaper is null || IsOwnFile(wallpaper, _dirPath))
                continue;

            var line = $"{id}\t{wallpaper}";
            var index = lines.FindIndex(l =>
                l.StartsWith(id + "\t", StringComparison.OrdinalIgnoreCase)
            );

            if (index < 0)
            {
                lines.Add(line);
                isChanged = true;
            }
            else if (!string.Equals(lines[index], line, StringComparison.Ordinal))
            {
                lines[index] = line;
                isChanged = true;
            }
        }

        if (isChanged)
            File.WriteAllLines(filePath, lines);
    }

    /// <summary>
    /// Puts back the user's own wallpaper and accent color (if they were replaced) and deletes the
    /// images, e.g. before Afterglow is uninstalled. Doesn't need Afterglow to be running.
    /// </summary>
    public static void RestoreDesktop()
    {
        var dirPath = GetDirPath();

        try
        {
            RestoreAccentColor(dirPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Dynamic wallpaper: {ex}");
        }

        try
        {
            RestoreWallpapers(dirPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Dynamic wallpaper: {ex}");
        }
    }

    private static void RestoreWallpapers(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            return;

        var filePath = Path.Combine(dirPath, OriginalWallpapersFileName);
        if (File.Exists(filePath))
        {
            using var desktop = DesktopWallpaper.TryCreate();
            if (desktop is null)
                return;

            var originals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Split('\t', 2);
                if (parts.Length != 2)
                    continue;

                if (parts[0] == "position")
                {
                    if (int.TryParse(parts[1], out var position))
                        desktop.Position = position;

                    continue;
                }

                originals[parts[0]] = parts[1];
            }

            // (displays that aren't connected right now keep what they had)
            var connected = desktop.GetMonitors().Select(m => m.Id).ToArray();
            var images = connected
                .Select(id => (Id: id, Path: originals.GetValueOrDefault(id)))
                .ToArray();

            // A solid color (which Windows only has for every display at once), if that's what
            // the displays had (the ones it's known for)
            var known = images.Where(i => i.Path is not null).ToArray();
            if (known.Length > 0 && known.All(i => i.Path == ""))
            {
                DesktopWallpaper.ShowSolidColor();
            }
            else
            {
                foreach (var (id, path) in images)
                {
                    // An image that's been deleted or moved meanwhile, or a display that was only
                    // ever seen with a sky, gets Windows' own default wallpaper (rather than being
                    // left pointing at a sky that's about to be deleted)
                    var image =
                        !string.IsNullOrEmpty(path) && File.Exists(path)
                            ? path
                            : DesktopWallpaper.TryGetDefaultWallpaper();

                    if (image is not null)
                        desktop.SetWallpaper(id, image);
                }
            }

            File.Delete(filePath);
        }

        // The images aren't kept while it's off (they're drawn again when it's turned on), other
        // than one that a display still shows (e.g. if there's nothing to put back instead), so
        // that no display is left pointing at a file that's gone
        var shown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var desktop = DesktopWallpaper.TryCreate())
        {
            foreach (var (id, _) in desktop?.GetMonitors() ?? [])
            {
                if (desktop?.TryGetWallpaper(id) is { Length: > 0 } wallpaper)
                    shown.Add(Path.GetFullPath(wallpaper));
            }
        }

        foreach (var imagePath in Directory.EnumerateFiles(dirPath))
        {
            if (
                (
                    imagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || imagePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    || imagePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                ) && !shown.Contains(Path.GetFullPath(imagePath))
            )
                TryDelete(imagePath);
        }
    }

    private static void RestoreAccentColor(string dirPath)
    {
        TryDelete(Path.Combine(dirPath, AppliedAccentColorFileName));

        var filePath = Path.Combine(dirPath, OriginalAccentColorFileName);
        if (!File.Exists(filePath))
            return;

        AccentColor.Restore(File.ReadAllText(filePath));
        File.Delete(filePath);
    }

    // The accent color this set last (as Windows stores it), if it's known
    private static uint? TryReadAppliedAccentColor(string dirPath)
    {
        try
        {
            var filePath = Path.Combine(dirPath, AppliedAccentColorFileName);
            return
                File.Exists(filePath)
                && uint.TryParse(
                    File.ReadAllText(filePath).Trim(),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var value
                )
                ? value
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // The accent color this set last, if it's still the one Windows has (e.g. after a restart),
    // so that it isn't set again (stalling everything for a moment) when it's still right
    private WallpaperColor? TryGetStillAppliedAccentColor()
    {
        if (TryReadAppliedAccentColor(_dirPath) is not { } value)
            return null;

        if (AccentColor.TryGetCurrent() != value)
            return null;

        // (stored as 0xAABBGGRR)
        return new WallpaperColor(
            (byte)(value & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)((value >> 16) & 0xFF)
        );
    }

    // Sets the accent color, after remembering the user's own (the first time, and again whenever
    // it isn't the one this set last, i.e. the user chose another one meanwhile). Returns whether
    // it took.
    private bool SetAccentColor(WallpaperColor color)
    {
        Directory.CreateDirectory(_dirPath);

        var originalPath = Path.Combine(_dirPath, OriginalAccentColorFileName);
        var appliedPath = Path.Combine(_dirPath, AppliedAccentColorFileName);

        // (only known to be the user's own if it's known what this set last, e.g. not right
        // after updating from a version that didn't keep track of it)
        var lastApplied = TryReadAppliedAccentColor(_dirPath);

        if (
            !File.Exists(originalPath)
            || (
                lastApplied is not null
                && AccentColor.TryGetCurrent() is { } current
                && current != lastApplied
            )
        )
        {
            File.WriteAllText(originalPath, AccentColor.Save());
        }

        if (!AccentColor.Set(color.Red, color.Green, color.Blue))
            return false;

        File.WriteAllText(
            appliedPath,
            AccentColor
                .ToValue(color.Red, color.Green, color.Blue)
                .ToString(CultureInfo.InvariantCulture)
        );

        return true;
    }

    /// <summary>
    /// Draws the sky (from the top down to the horizon), a glow along the horizon, and a brighter
    /// glow in the middle of it (like the sun just below the horizon), and saves it as a PNG.
    /// </summary>
    /// <remarks>
    /// Drawn with full precision and then dithered (with a fine, repeating pattern before
    /// rounding to 8 bits), as smooth gradients over a large screen otherwise show bands: a night
    /// sky only spans a few dozen levels from top to bottom. The pattern hides them as well as
    /// noise would, but compresses far better (about 1 MB for a 4K image, rather than 7 MB).
    /// </remarks>
    internal static void Render(
        WallpaperPalette palette,
        int width,
        int height,
        string filePath,
        int maxThreads
    ) => File.Move(RenderToTempFile(palette, width, height, filePath, maxThreads), filePath, true);

    // Draws it into a file of its own next to the given one (written in full before Windows is
    // pointed at it, and never in the way of another one being drawn at the same time), and
    // returns its path
    private static string RenderToTempFile(
        WallpaperPalette palette,
        int width,
        int height,
        string filePath,
        int maxThreads
    )
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var bitmap = new SKBitmap(info);
        var pixels = bitmap.GetPixels();
        var rowBytes = bitmap.RowBytes;

        static (float R, float G, float B) ToVector(WallpaperColor c) => (c.Red, c.Green, c.Blue);

        var top = ToVector(palette.SkyTop);
        var middle = ToVector(palette.SkyMiddle);
        var horizon = ToVector(palette.SkyHorizon);
        var glow = ToVector(palette.Glow);
        var strength = (float)palette.GlowStrength;

        // The middle glow: an ellipse, wider than it's tall, centered just below the bottom edge
        var centerX = width / 2f;
        var centerY = height * 1.02f;
        var radiusX = Math.Max(width, height * 0.6f) * 0.75f;
        var radiusY = height * 0.5f;

        void DrawRow(int y, byte[] row)
        {
            var v = height > 1 ? (float)y / (height - 1) : 0;

            // The sky, from the top to the middle (a little over halfway) to the horizon
            var (skyFrom, skyTo, skyAmount) =
                v <= 0.55f ? (top, middle, v / 0.55f) : (middle, horizon, (v - 0.55f) / 0.45f);
            var skyR = skyFrom.R + (skyTo.R - skyFrom.R) * skyAmount;
            var skyG = skyFrom.G + (skyTo.G - skyFrom.G) * skyAmount;
            var skyB = skyFrom.B + (skyTo.B - skyFrom.B) * skyAmount;

            // The band along the horizon, over the lower 60%
            var bandAlpha = 0f;
            if (v > 0.4f)
            {
                var u = (v - 0.4f) / 0.6f;
                bandAlpha =
                    u <= 0.6f
                        ? strength * 0.4f * (u / 0.6f)
                        : strength * (0.4f + 0.5f * (u - 0.6f) / 0.4f);
            }

            skyR += (glow.R - skyR) * bandAlpha;
            skyG += (glow.G - skyG) * bandAlpha;
            skyB += (glow.B - skyB) * bandAlpha;

            var dy = (y - centerY) / radiusY;

            for (var x = 0; x < width; x++)
            {
                var dx = (x - centerX) / radiusX;
                var t = MathF.Sqrt(dx * dx + dy * dy);

                var alpha =
                    t >= 1 ? 0
                    : t <= 0.45f ? strength * (1 - 0.55f * t / 0.45f)
                    : strength * 0.45f * (1 - (t - 0.45f) / 0.55f);

                var i = x * 4;
                row[i] = Dither(skyR + (glow.R - skyR) * alpha, x, y, 0);
                row[i + 1] = Dither(skyG + (glow.G - skyG) * alpha, x, y, 1);
                row[i + 2] = Dither(skyB + (glow.B - skyB) * alpha, x, y, 2);
                row[i + 3] = 255;
            }

            Marshal.Copy(row, 0, pixels + y * rowBytes, row.Length);
        }

        if (maxThreads <= 1)
        {
            var row = new byte[width * 4];
            for (var y = 0; y < height; y++)
                DrawRow(y, row);
        }
        else
        {
            Parallel.For(
                0,
                height,
                new ParallelOptions { MaxDegreeOfParallelism = maxThreads },
                () => new byte[width * 4],
                (y, _, row) =>
                {
                    DrawRow(y, row);
                    return row;
                },
                _ => { }
            );
        }

        // Fast compression (smaller files wouldn't be worth the time)
        using var pixmap = bitmap.PeekPixels();
        using var data =
            pixmap.Encode(new SKPngEncoderOptions(SKPngEncoderFilterFlags.Sub, 1))
            ?? throw new InvalidOperationException("Failed to encode the wallpaper.");

        var tempFilePath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        using (var stream = File.Create(tempFilePath))
            data.SaveTo(stream);

        return tempFilePath;
    }

    // 8×8 ordered-dither thresholds (from 0 to 1)
    private static readonly float[] Bayer = CreateBayer();

    private static float[] CreateBayer()
    {
        int[] quadrants = [0, 2, 3, 1];
        var matrix = new float[64];

        for (var y = 0; y < 8; y++)
        for (var x = 0; x < 8; x++)
        {
            var value = 0;
            for (var bit = 0; bit < 3; bit++)
                value = value * 4 + quadrants[((y >> (2 - bit)) & 1) * 2 + ((x >> (2 - bit)) & 1)];

            matrix[y * 8 + x] = (value + 0.5f) / 64;
        }

        return matrix;
    }

    // Rounds a level (0 to 255) up or down by the pattern, with each channel's pattern offset so
    // that they don't line up into a visible texture
    private static byte Dither(float value, int x, int y, int channel)
    {
        var threshold = Bayer[((y + channel * 3) & 7) * 8 + ((x + channel * 5) & 7)];
        return (byte)Math.Clamp(MathF.Floor(value + threshold), 0, 255);
    }

    public void Dispose()
    {
        _timer?.Stop();
        _drawingCancellation?.Cancel();
        _settingsSubscription.Dispose();
        _sessionSubscription?.Dispose();
        _gpuUsage.Dispose();
    }
}
