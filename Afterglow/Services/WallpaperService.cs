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
/// <param name="Stage">The part of the cycle (for the accent color).</param>
/// <param name="SunElevation">How high the sun is, in degrees (negative below the horizon).</param>
/// <param name="IsImmediate">Whether it's shown right away (when stepping through the day).</param>
/// <param name="Weather">The weather the sky shows (clear, unless that's turned on).</param>
public readonly record struct WallpaperTarget(
    CycleStage Stage,
    double SunElevation,
    bool IsImmediate = false,
    SkyConditions Weather = default
);

/// <summary>
/// What the dynamic wallpaper shows (for the dashboard).
/// </summary>
/// <param name="To">The weather it shows, or is on its way to.</param>
/// <param name="From">The weather it showed before, while it's on its way (null once it's there,
/// or if that isn't known).</param>
/// <param name="IsWaiting">Whether it's waiting for the weather (e.g. at startup, until the forecast
/// is there).</param>
public readonly record struct WallpaperStatus(
    SkyKind To,
    SkyKind? From = null,
    bool IsWaiting = false
);

/// <summary>
/// A sky that a display shows: one of the clear skies drawn ahead of time (by its stop, see
/// <see cref="DynamicWallpaper.GetStops" />, and how high up the user is, see <see
/// cref="AltitudeLevels" />), or one drawn when it's needed (for the weather, or on the way to or
/// from it).
/// </summary>
internal readonly record struct WallpaperSky(WallpaperPalette Palette, int? Stop, int Altitude = 0)
{
    /// <summary>
    /// How finely the clear skies follow how high up the user is (see <see
    /// cref="SkyConditions.Altitude" />): a set of them for each level (about 440 m apart), drawn
    /// for where the user is. Level 0 (up to about 500 m, where most people live) is the plain
    /// one.
    /// </summary>
    public const int AltitudeLevels = 8;

    public static int GetAltitudeLevel(double altitude) =>
        (int)
            Math.Round(Math.Clamp(double.IsFinite(altitude) ? altitude : 0, 0, 1) * AltitudeLevels);

    public static SkyConditions GetClearConditions(int altitude) =>
        new(Altitude: (double)altitude / AltitudeLevels);

    public static WallpaperSky Clear(int stop, int altitude = 0) =>
        new(
            altitude == 0
                ? DynamicWallpaper.GetPalette(DynamicWallpaper.GetStops()[stop])
                : DynamicWallpaper.GetPalette(
                    DynamicWallpaper.GetStops()[stop],
                    GetClearConditions(altitude)
                ),
            stop,
            altitude
        );

    // (with the glow's strength kept to what a file name holds, see GetImageFileName, so that the
    // image and its name always agree)
    public static WallpaperSky Drawn(WallpaperPalette palette) =>
        new(palette with { GlowStrength = Math.Round(palette.GlowStrength * 255) / 255 }, null);
}

/// <summary>
/// Sets the desktop wallpaper to a sky for where the sun is and the weather (see <see
/// cref="DynamicWallpaper" />), and the Windows accent color to a color of that sky for the part
/// of the day.
/// </summary>
/// <remarks>
/// <para>
/// The images are natural skies, which only depend on how high the sun is and the weather: the
/// screen's tint warms and dims them live, like everything else on the screen. The clear skies
/// (one for each of the stops along the sun's path, shared by the morning and the evening) are
/// drawn once for each display size, the one that's needed first right away and the others one at
/// a time in the background, so that following the sun is just pointing Windows at another
/// image. A sky with weather is drawn when it's needed (in well under a second, at the lowest
/// priority), and only kept while a display shows it.
/// </para>
/// <para>
/// The goal is that the user never sees the wallpaper change. Every change is a small step (no
/// bigger than from one clear sky to the next, see <see cref="DynamicWallpaper.StepSize" />), so a
/// change of the weather, or a display catching up, goes one step at a time, never in a jump. And
/// each display only takes a step when nobody's likely to be looking at its desktop:
/// <list type="bullet">
/// <item>While nobody can see the screen at all (it's locked, the display is off, or a screensaver
/// runs), it goes straight to the right sky.</item>
/// <item>While a window covers the whole display (other than the taskbar), the desktop can't be
/// seen, so it steps along briskly (still in steps, as some windows show a blur of the wallpaper
/// through them).</item>
/// <item>Otherwise it waits until the user has been away from that display for a while (working on
/// another one, or not using the computer), and hasn't just uncovered its desktop (e.g. by
/// minimizing a window, when they're likely to be looking at it), and then takes steps a minute
/// or two apart.</item>
/// <item>Never under a fullscreen application (a game, or a video), and nothing is drawn in the
/// background meanwhile.</item>
/// </list>
/// A sky that takes a moment to draw is only shown if that's all still true once it's drawn.
/// What the user is doing is only looked up while a display has a step to take, and at a slow
/// pace, so that following the sun costs next to nothing.
/// </para>
/// <para>
/// The accent color has three colors a day (see <see cref="DynamicWallpaper.GetAccentTime" />),
/// and only changes while nobody can see it: while the screen is locked, the display is off, or a
/// screensaver runs. Windows has every application re-theme at once when it changes, which stalls
/// them all (and, through the applications that watch the mouse, the mouse too) for up to a couple
/// of seconds, besides changing the taskbar and title bars in front of the user. If nobody steps
/// away for a while, it just waits. The only exceptions are turning it on and stepping through the
/// day, which show it right away.
/// </para>
/// <para>
/// Stepping through the day (by hand, to see how it all looks) shows the sky and the accent color
/// right away, whatever it costs, and stopping it puts them back right away.
/// </para>
/// <para>
/// Images are drawn at each display's own resolution (so Windows doesn't scale them), dithered
/// (so gradients don't show bands), and saved as PNG (as Windows compresses JPEG wallpapers again,
/// which brings the bands back).
/// </para>
/// </remarks>
public class WallpaperService : IDisposable
{
    // Checked often while the user just changed something (or something's being drawn), and
    // rarely otherwise (the sun moves slowly, and a step waits for a quiet moment anyway), so
    // that it wakes up as little as possible
    private static readonly TimeSpan FastCheckInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan SlowCheckInterval = TimeSpan.FromSeconds(15);

    // How long checks stay frequent after the user changes something
    private static readonly TimeSpan FastCheckTime = TimeSpan.FromSeconds(30);

    // A display the user is working on (where the mouse or the window in focus is) counts as
    // being worked on until there's been no input for this long
    private static readonly TimeSpan QuietTime = TimeSpan.FromMinutes(2);

    // How long a display waits after the user was last working on it, and after its desktop was
    // last covered, before its (visible) desktop takes a step
    private static readonly TimeSpan AwayTime = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan UncoveredTime = TimeSpan.FromMinutes(2);

    // How far apart steps are, on a desktop that can be seen and on a covered one
    private static readonly TimeSpan VisibleStepInterval = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan CoveredStepInterval = TimeSpan.FromSeconds(30);

    // How long the background drawing waits while a video or a game is playing
    private static readonly TimeSpan BusyRetryDelay = TimeSpan.FromSeconds(30);

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
    private IReadOnlyList<Rect> _coveredBounds = [];
    private bool _isLocked;
    private bool _isUnseen;
    private volatile bool _isDrawingPaused;
    private DateTimeOffset _activityCheckedAt = DateTimeOffset.MinValue;

    // Whether the display is off (as Windows reports it, when it changes)
    private bool _isDisplayOff;

    // Each display (by its bounds), what it shows, and when the user was last on it
    private sealed class Display(string id)
    {
        public string Id { get; set; } = id;

        // (null for anything other than these skies, e.g. the user's own wallpaper)
        public WallpaperSky? Shown { get; set; }
        public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.MinValue;
        public DateTimeOffset BusyAt { get; set; } = DateTimeOffset.MinValue;
        public DateTimeOffset CoveredAt { get; set; } = DateTimeOffset.MinValue;
    }

    private readonly Dictionary<Rect, Display> _displays = [];

    // Whether the displays (and what they show) need to be looked up again, e.g. when one is
    // connected
    private bool _isRefreshNeeded = true;

    private WallpaperColor? _appliedAccentColor;

    // Whether the accent color was just turned on (so it's shown right away, once), and whether it
    // was on before (settings are re-announced on every save, so a change is told apart this way)
    private bool _isAccentRequested;
    private bool _wasImmediate;
    private bool _wasAccentColorMatched;

    // The weather that every display last got to (to say where a change comes from)
    private SkyKind? _reachedKind;

    /// <summary>
    /// What the wallpaper shows (null while it's off, or before it's started).
    /// </summary>
    public WallpaperStatus? Status { get; private set; }

    // Whether the sky goes straight to the right one once: when the app starts, the computer wakes
    // up, the user changes something about it (see ShowNow), or stops stepping through the day
    private bool _isJumpRequested = true;

    // How high up the user is (the clear skies drawn ahead are for that, see WallpaperSky)
    private volatile int _altitude;

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

        _settingsSubscription = settingsService.WatchProperties(
            [o => o.IsDynamicWallpaperEnabled, o => o.IsAccentColorMatched],
            Evaluate
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

        // The screen being locked (or waking up to the lock screen), and the display turning off,
        // are when the accent color can change (and the sky catch up at once), so they're checked
        // right away rather than at the regular pace (which could miss a quick unlock)
        _sessionSubscription = PowerKit.Disposable.Merge(
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.ConsoleDisplayStateChanged,
                state =>
                {
                    // (0 is off, 1 on, 2 dimmed)
                    _isDisplayOff = state == 0;
                    if (_isDisplayOff)
                        NotifyTargetChanged();
                }
            ) ?? PowerKit.Disposable.Null,
            SessionEvents.RegisterLock(isLocked =>
            {
                if (isLocked)
                    NotifyTargetChanged();
            }),
            SessionEvents.RegisterSleep(isSuspending =>
            {
                if (!isSuspending)
                {
                    _isRefreshNeeded = true;
                    _isJumpRequested = true;

                    // (after anything else that's woken up, e.g. the weather, which may make it
                    // wait for a fresh forecast)
                    Dispatcher.UIThread.Post(NotifyTargetChanged, DispatcherPriority.Background);
                }
            }),
            // A display connected, disconnected, or changed (e.g. its resolution)
            SystemEvent.Register(
                SystemEvent.Ids.DisplayChanged,
                () =>
                {
                    _isRefreshNeeded = true;
                    NotifyTargetChanged();
                }
            )
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
            _displays.Clear();
            _isRefreshNeeded = true;
            _reachedKind = null;
            Status = null;
            _appliedAccentColor = null;
            _isAccentRequested = false;
            _isJumpRequested = false;
            _wasAccentColorMatched = false;

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
    /// Shows the right sky right away, on every display (rather than in small steps, in quiet
    /// moments), e.g. when the user changed something about it, so that they can see it working.
    /// </summary>
    public void ShowNow()
    {
        _isJumpRequested = true;
        NotifyTargetChanged();
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

    // Only looked up while there's something to do, as it opens the foreground application (and,
    // if a display might take a step, goes over the windows to see which displays they cover)
    private void UpdateActivity(bool isCoverageNeeded)
    {
        var now = DateTimeOffset.Now;

        // Not looked at for a while (nothing was waiting): what the user did meanwhile isn't known,
        // so they may have just been on any display (or uncovered it)
        var isHistoryKnown = now - _activityCheckedAt <= SlowCheckInterval * 2;
        _activityCheckedAt = now;

        // The cheapest first: while the screen is locked, the user is away, so the foreground
        // application isn't looked up (and it's the best time to draw)
        _isLocked = InputDesktop.IsLocked();
        if (_isLocked)
        {
            _isUnseen = true;
            _isDrawingPaused = false;
            return;
        }

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

        // The displays being worked on (where the mouse or the window in focus is, until there's
        // been no input for a while)
        _busyBounds =
            UserActivity.GetIdleTime() < QuietTime ? UserActivity.GetActiveMonitorBounds() : [];

        _coveredBounds = isCoverageNeeded ? DesktopCoverage.GetCoveredMonitorBounds() : [];

        foreach (var (bounds, display) in _displays)
        {
            if (!isHistoryKnown || _busyBounds.Contains(bounds))
                display.BusyAt = now;
            if (!isHistoryKnown || _coveredBounds.Contains(bounds))
                display.CoveredAt = now;
        }

        // The accent color only changes while nobody can see it (see the remarks). Drawing (at the
        // lowest priority) only waits for a fullscreen application.
        _isUnseen = _isDisplayOff || UserActivity.IsScreenSaverRunning();
        _isDrawingPaused = _fullScreenBounds is not null;
    }

    // The sky that should be shown: the clear one for where the sun is (and how high up the user
    // is), unless there's enough weather to show (and then one drawn for it)
    private static WallpaperSky GetTargetSky(WallpaperTarget target)
    {
        var altitude = WallpaperSky.GetAltitudeLevel(target.Weather.Altitude);
        var clearConditions = WallpaperSky.GetClearConditions(altitude);
        var clear = WallpaperSky.Clear(
            DynamicWallpaper.GetStopIndex(target.SunElevation),
            altitude
        );

        // (the altitude as the clear skies have it, so that the two agree)
        var weather = target.Weather with
        {
            Altitude = clearConditions.Altitude,
        };
        if (weather == clearConditions)
            return clear;

        var palette = DynamicWallpaper.GetPalette(target.SunElevation, weather);

        // (so little weather that it wouldn't show: the clear sky, which is drawn already)
        return DynamicWallpaper.GetDifference(palette, clear.Palette) < DynamicWallpaper.StepSize
            ? clear
            : WallpaperSky.Drawn(palette);
    }

    // Whether a display showing the one sky should move towards the other: always for a clear sky
    // (which is exact), and for one with weather once it's a whole step away (as it moves along
    // with the sun and the weather all the time)
    private static bool IsStepNeeded(WallpaperSky? shown, WallpaperSky target) =>
        shown is not { } sky
        || (
            target.Stop is not null
                ? sky != target
                : DynamicWallpaper.GetDifference(sky.Palette, target.Palette)
                    >= DynamicWallpaper.StepSize
        );

    // The next step from the one sky towards the other: from one clear sky to the next, or a step's
    // worth of the way (and the target itself, once it's that close)
    private static WallpaperSky GetNextSky(WallpaperSky shown, WallpaperSky target)
    {
        if (shown.Stop is { } from && target.Stop is { } to && shown.Altitude == target.Altitude)
            return WallpaperSky.Clear(from + Math.Sign(to - from), target.Altitude);

        var palette = DynamicWallpaper.StepTowards(
            shown.Palette,
            target.Palette,
            DynamicWallpaper.StepSize
        );

        return palette == target.Palette ? target : WallpaperSky.Drawn(palette);
    }

    // How a display may change now, if at all (see the remarks)
    private enum ChangeKind
    {
        None,
        Jump,
        Step,
    }

    private ChangeKind GetChangeKind(Rect bounds, Display display, bool isImmediate)
    {
        // Stepping through the day, turned on just now (or the display shows something else), or
        // nobody can see the screen
        if (isImmediate || display.Shown is null || _isLocked || _isUnseen)
            return ChangeKind.Jump;

        if (_fullScreenBounds == bounds)
            return ChangeKind.None;

        var now = DateTimeOffset.Now;
        var sinceChange = now - display.ChangedAt;

        if (_coveredBounds.Contains(bounds))
            return sinceChange >= CoveredStepInterval ? ChangeKind.Step : ChangeKind.None;

        return
            sinceChange >= VisibleStepInterval
            && !_busyBounds.Contains(bounds)
            && now - display.BusyAt >= AwayTime
            && now - display.CoveredAt >= UncoveredTime
            ? ChangeKind.Step
            : ChangeKind.None;
    }

    private void Check()
    {
        if (!IsEnabled || _isBusy || _getTarget?.Invoke() is not { } target)
            return;

        // The displays (and what they show) are looked up first
        if (_isRefreshNeeded || _displays.Count == 0)
        {
            SetCheckInterval(true);
            Apply([], null, false);
            return;
        }

        var targetSky = GetTargetSky(target);

        // (so that the clear skies for this altitude are kept, and drawn ahead)
        _altitude = targetSky.Altitude;

        var accent = DynamicWallpaper.GetAccentColor(
            DynamicWallpaper.GetAccentTime(target.Stage, target.SunElevation)
        );

        // Stepping through the day is for seeing how it all looks, so the sky and the accent color
        // are shown right away, whatever it costs, and they go back to the time of day right away
        // once that stops
        if (_wasImmediate && !target.IsImmediate)
        {
            _isAccentRequested = true;
            _isJumpRequested = true;
        }
        _wasImmediate = target.IsImmediate;
        var isImmediate = target.IsImmediate || _isJumpRequested;
        var isAccentForced = _isAccentRequested || target.IsImmediate;

        var pending = _displays.Where(d => IsStepNeeded(d.Value.Shown, targetSky)).ToArray();

        // (a sky with so little weather that it's drawn clear is called clear)
        var kind = targetSky.Stop is not null ? SkyKind.Clear : target.Weather.Kind;
        if (pending.Length == 0)
            _reachedKind = kind;
        Status = new WallpaperStatus(kind, _reachedKind != kind ? _reachedKind : null);
        var isAccentNeeded = IsAccentColorMatched && _appliedAccentColor != accent;

        // Checked often only while something's being drawn (to stop for a fullscreen application)
        // or was asked for just now: a step waits for a quiet moment, which takes a while anyway
        SetCheckInterval(!_drawing.IsCompleted || _isAccentRequested || isImmediate);

        // (also keeps the background drawing informed)
        if (pending.Length == 0 && !isAccentNeeded && _drawing.IsCompleted)
        {
            _isJumpRequested = false;
            return;
        }

        // The windows are only gone over if a covered display could take a step now
        var now = DateTimeOffset.Now;
        UpdateActivity(
            !isImmediate
                && pending.Any(d =>
                    d.Value.Shown is not null && now - d.Value.ChangedAt >= CoveredStepInterval
                )
        );

        // The accent color waits for nobody to be able to see it (unless it's forced, see above)
        if (isAccentNeeded && !isAccentForced && !_isUnseen && !_isLocked)
            isAccentNeeded = false;

        var changes = new List<(Rect Bounds, WallpaperSky Sky)>();
        foreach (var (bounds, display) in pending)
        {
            switch (GetChangeKind(bounds, display, isImmediate))
            {
                case ChangeKind.Jump:
                    changes.Add((bounds, targetSky));
                    break;
                case ChangeKind.Step:
                    changes.Add((bounds, GetNextSky(display.Shown!.Value, targetSky)));
                    break;
            }
        }

        _isJumpRequested = false;

        if (changes.Count > 0 || isAccentNeeded)
            Apply(changes, isAccentNeeded ? accent : null, isImmediate);
    }

    private async void Apply(
        IReadOnlyList<(Rect Bounds, WallpaperSky Sky)> changes,
        WallpaperColor? accent,
        bool isImmediate
    )
    {
        _isBusy = true;
        var applied = new List<(Rect Bounds, WallpaperSky Sky)>();
        var isAccentApplied = false;
        IReadOnlyList<(string Id, Rect Bounds, WallpaperSky? Shown)>? displays = null;

        try
        {
            // The images that aren't there yet are drawn first, without holding anything up
            var sizes = changes.Select(c => (Size: GetSize(c.Bounds), c.Sky)).Distinct().ToArray();
            var isDrawn = await Task.Run(() =>
            {
                var drew = false;
                foreach (var (size, sky) in sizes)
                    drew |= EnsureImage(size, sky);
                return drew;
            });

            // A step that took a moment to draw is only taken if the display is still as quiet as
            // it was (e.g. the user didn't just come back to it)
            if (isDrawn && !isImmediate && IsEnabled)
            {
                UpdateActivity(_coveredBounds.Count > 0);
                changes = changes
                    .Where(c =>
                        _displays.TryGetValue(c.Bounds, out var display)
                        && GetChangeKind(c.Bounds, display, false) != ChangeKind.None
                    )
                    .ToArray();
            }

            if (!IsEnabled)
                return;

            await _lock.WaitAsync();
            try
            {
                (applied, isAccentApplied, displays) = await Task.Run(() =>
                    ApplyCore(changes, accent)
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

        if (displays is not null)
            UpdateDisplays(displays);

        var now = DateTimeOffset.Now;
        foreach (var (bounds, sky) in applied)
        {
            if (_displays.TryGetValue(bounds, out var display))
            {
                display.Shown = sky;
                display.ChangedAt = now;
            }
        }

        if (accent is not null)
        {
            // (if it didn't take, it's tried again the next time nobody can see it)
            if (isAccentApplied)
                _appliedAccentColor = accent;
            _isAccentRequested = false;
        }

        // The clear skies that aren't drawn yet, closest to the one for where the sun is first
        if (_getTarget?.Invoke() is { } target)
        {
            StartDrawing(
                DynamicWallpaper.GetStopIndex(target.SunElevation),
                WallpaperSky.GetAltitudeLevel(target.Weather.Altitude)
            );
        }

        // (a display that was just looked up may need a step already)
        if (changes.Count == 0 && accent is null && !_isRefreshNeeded)
            Check();
    }

    private static (int Width, int Height) GetSize(Rect bounds) =>
        (bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);

    // e.g. "Sky 4 1920x1080 23.png" for a clear sky ("Sky 4 1920x1080 h3 23.png" high up), and
    // "Sky 4 1920x1080 1a2b3c….png" for one drawn when it's needed (with its colors in the name, so
    // that it's known again, e.g. after a restart)
    internal static string GetImageFileName((int Width, int Height) size, WallpaperSky sky)
    {
        var name = sky.Stop is { } stop
            ? (sky.Altitude > 0 ? $"h{sky.Altitude} {stop:00}" : $"{stop:00}")
            : string.Concat(
                new[]
                {
                    sky.Palette.SkyTop,
                    sky.Palette.SkyMiddle,
                    sky.Palette.SkyHorizon,
                    sky.Palette.Glow,
                }.Select(c => $"{c.Red:x2}{c.Green:x2}{c.Blue:x2}")
            ) + $"{(int)Math.Round(sky.Palette.GlowStrength * 255):x2}";

        return $"{ImagePrefix}{size.Width}x{size.Height} {name}.png";
    }

    private string GetImagePath((int Width, int Height) size, WallpaperSky sky) =>
        Path.Combine(_dirPath, GetImageFileName(size, sky));

    /// <summary>
    /// Gets the sky in one of these images, from its file name (see <see cref="GetImageFileName"
    /// />), or null if it isn't one.
    /// </summary>
    internal static WallpaperSky? TryParseImageFileName(string fileName)
    {
        if (
            !fileName.StartsWith(ImagePrefix, StringComparison.Ordinal)
            || !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
        )
            return null;

        var parts = fileName[ImagePrefix.Length..^".png".Length].Split(' ');

        static int? TryParseNumber(string text, int max) =>
            text.Length is > 0 and <= 2
            && text.All(char.IsAsciiDigit)
            && int.Parse(text, CultureInfo.InvariantCulture) is var number
            && number < max
                ? number
                : null;

        // (a clear sky high up)
        if (
            parts.Length == 3
            && parts[1].StartsWith('h')
            && TryParseNumber(parts[1][1..], WallpaperSky.AltitudeLevels + 1) is { } altitude
            && altitude > 0
            && parts[2].Length == 2
            && TryParseNumber(parts[2], DynamicWallpaper.StopCount) is { } highStop
        )
            return WallpaperSky.Clear(highStop, altitude);

        if (parts.Length != 2)
            return null;

        var name = parts[1];
        if (name.Length == 2 && TryParseNumber(name, DynamicWallpaper.StopCount) is { } stop)
            return WallpaperSky.Clear(stop);

        if (name.Length != 26 || !name.All(char.IsAsciiHexDigitLower))
            return null;

        var bytes = Convert.FromHexString(name);
        WallpaperColor ColorAt(int i) => new(bytes[i], bytes[i + 1], bytes[i + 2]);

        return WallpaperSky.Drawn(
            new WallpaperPalette(ColorAt(0), ColorAt(3), ColorAt(6), ColorAt(9), bytes[12] / 255.0)
        );
    }

    // Draws the image if it isn't there yet (at the lowest priority, so that it never gets in the
    // way, even if it takes a moment longer), and returns whether it had to
    private bool EnsureImage((int Width, int Height) size, WallpaperSky sky)
    {
        var filePath = GetImagePath(size, sky);
        if (File.Exists(filePath))
            return false;

        Directory.CreateDirectory(_dirPath);

        using var backgroundMode = BackgroundWork.EnterBackgroundMode();
        var tempFilePath = RenderToTempFile(sky.Palette, size.Width, size.Height, filePath, 1);
        try
        {
            File.Move(tempFilePath, filePath, true);
        }
        finally
        {
            TryDelete(tempFilePath);
        }

        return true;
    }

    // Shows the given skies (on the displays that are still there), and sets the accent color.
    // Returns the skies that were shown, whether the accent color took, and the displays and what
    // they show now (null if they couldn't be looked up).
    private (
        List<(Rect Bounds, WallpaperSky Sky)> Applied,
        bool IsAccentApplied,
        IReadOnlyList<(string Id, Rect Bounds, WallpaperSky? Shown)>? Displays
    ) ApplyCore(IReadOnlyList<(Rect Bounds, WallpaperSky Sky)> changes, WallpaperColor? accent)
    {
        var applied = new List<(Rect, WallpaperSky)>();
        IReadOnlyList<(string, Rect, WallpaperSky?)>? displays = null;

        if (DesktopWallpaper.TryCreate() is { } desktop)
        {
            using var _ = desktop;
            var monitors = desktop.GetMonitors();

            if (changes.Count > 0)
            {
                Directory.CreateDirectory(_dirPath);
                RememberOriginalWallpapers(desktop, monitors);
                DeleteOutdatedImages();
            }

            foreach (var (bounds, sky) in changes)
            {
                var monitor = monitors.FirstOrDefault(m => m.Bounds == bounds);
                if (monitor.Id is null)
                    continue;

                var filePath = GetImagePath(GetSize(bounds), sky);

                // (drawn just before, unless it was deleted meanwhile)
                if (!File.Exists(filePath))
                    continue;

                if (
                    !string.Equals(
                        desktop.TryGetWallpaper(monitor.Id),
                        filePath,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                    desktop.SetWallpaper(monitor.Id, filePath);

                applied.Add((bounds, sky));
            }

            if (changes.Count > 0 && desktop.Position != DesktopWallpaper.FillPosition)
                desktop.Position = DesktopWallpaper.FillPosition;

            displays = GetDisplays(desktop, monitors);

            if (changes.Count > 0)
                DeleteUnusedImages(desktop, monitors);
        }

        // (also when the sky couldn't be shown)
        var isAccentApplied = accent is { } color && SetAccentColor(color);

        return (applied, isAccentApplied, displays);
    }

    // The displays that are connected now, and what they show (the user may have chosen another
    // wallpaper meanwhile, which counts as nothing being shown)
    private IReadOnlyList<(string Id, Rect Bounds, WallpaperSky? Shown)> GetDisplays(
        DesktopWallpaper desktop,
        IReadOnlyList<(string Id, Rect Bounds)> monitors
    ) =>
        monitors
            .Select(m =>
            {
                var wallpaper = desktop.TryGetWallpaper(m.Id);
                var shown =
                    !string.IsNullOrEmpty(wallpaper)
                    && IsOwnFile(wallpaper, _dirPath)
                    && File.Exists(wallpaper)
                        ? TryParseImageFileName(Path.GetFileName(wallpaper))
                        : null;

                return (m.Id, m.Bounds, shown);
            })
            .ToArray();

    private void UpdateDisplays(
        IReadOnlyList<(string Id, Rect Bounds, WallpaperSky? Shown)> displays
    )
    {
        foreach (
            var bounds in _displays.Keys.Where(b => displays.All(d => d.Bounds != b)).ToArray()
        )
            _displays.Remove(bounds);

        foreach (var (id, bounds, shown) in displays)
        {
            if (!_displays.TryGetValue(bounds, out var display))
                _displays[bounds] = display = new Display(id);

            display.Id = id;
            display.Shown = shown;
        }

        _sizes = displays.Select(d => GetSize(d.Bounds)).Distinct().ToArray();
        _isRefreshNeeded = false;
    }

    // The skies drawn when they were needed are only kept while a display shows them (they're
    // drawn again if they're needed after all), and so are the clear skies for an altitude other
    // than the user's (e.g. after moving), and anything else (e.g. left over from an earlier
    // version)
    private void DeleteUnusedImages(
        DesktopWallpaper desktop,
        IReadOnlyList<(string Id, Rect Bounds)> monitors
    )
    {
        var shown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, _) in monitors)
        {
            if (desktop.TryGetWallpaper(id) is { Length: > 0 } wallpaper)
                shown.Add(Path.GetFileName(wallpaper));
        }

        foreach (var filePath in Directory.EnumerateFiles(_dirPath, ImagePrefix + "*.png"))
        {
            var fileName = Path.GetFileName(filePath);
            if (
                shown.Contains(fileName)
                || TryParseImageFileName(fileName) is { Stop: not null } clear
                    && (clear.Altitude == 0 || clear.Altitude == _altitude)
            )
                continue;

            TryDelete(filePath);
        }
    }

    // Draws the clear skies that aren't there yet, one at a time, closest to the given one first
    // (as they're needed soonest), and waiting while an application is fullscreen
    private void StartDrawing(int nearStop, int altitude)
    {
        _altitude = altitude;

        if (!_drawing.IsCompleted)
            return;

        var sizes = _sizes;
        var missing = sizes
            .SelectMany(size =>
                Enumerable
                    .Range(0, DynamicWallpaper.StopCount)
                    .Select(stop => (Size: size, Sky: WallpaperSky.Clear(stop, altitude)))
            )
            .Where(i => !File.Exists(GetImagePath(i.Size, i.Sky)))
            .OrderBy(i => Math.Abs(i.Sky.Stop!.Value - nearStop))
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

                foreach (var (size, sky) in missing)
                {
                    while (_isDrawingPaused)
                    {
                        if (token.WaitHandle.WaitOne(BusyRetryDelay))
                            return;
                    }

                    if (token.IsCancellationRequested)
                        return;

                    var filePath = GetImagePath(size, sky);
                    if (File.Exists(filePath))
                        continue;

                    // Drawn without holding anything up (a change of the wallpaper or the accent
                    // color doesn't wait for it), and only put in place while nothing else works
                    // with the files, so that it isn't left behind once this is turned off
                    string? tempFilePath = null;
                    try
                    {
                        tempFilePath = RenderToTempFile(
                            sky.Palette,
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
    }
}
