using System;
using System.Collections.Generic;
using System.Diagnostics;
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
/// A display covered by a fullscreen application (e.g. a game) keeps its sky until it's
/// uncovered (the others carry on), nothing is drawn in the background meanwhile, and the accent
/// color follows less often (as every application, the game included, is told when it changes).
/// Nothing changes while the screen is locked. It all catches up afterwards.
/// </para>
/// <para>
/// Images are drawn at each display's own resolution (so Windows doesn't scale them), dithered
/// (so gradients don't show bands), and saved as PNG (as Windows compresses JPEG wallpapers again,
/// which brings the bands back).
/// </para>
/// </remarks>
public class WallpaperService : IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(3);

    // How often the accent color follows the cycle's slow drift, at most
    private static readonly TimeSpan AccentDriftInterval = TimeSpan.FromSeconds(30);

    // ...and while an application is fullscreen
    private static readonly TimeSpan FullScreenAccentInterval = TimeSpan.FromMinutes(2);

    // How long the background drawing waits while an application is fullscreen
    private static readonly TimeSpan FullScreenRetryDelay = TimeSpan.FromSeconds(30);

    // Smaller changes of the accent color (in any channel, out of 255) go by unnoticed
    private const int NoticeableAccentDifference = 2;

    // Images drawn by an earlier version (e.g. with other skies) are told apart by this
    private const string ImagePrefix = "Sky 2 ";

    private const string OriginalWallpapersFileName = "Original wallpapers.txt";
    private const string OriginalAccentColorFileName = "Original accent color.txt";

    private readonly SettingsService _settingsService;
    private readonly ExternalApplicationService _externalApplicationService;
    private readonly string _dirPath;
    private readonly IDisposable _settingsSubscription;

    // All work with the files, the wallpaper and the accent color happens one piece at a time
    private readonly SemaphoreSlim _lock = new(1, 1);

    private Func<WallpaperTarget?>? _getTarget;
    private DispatcherTimer? _timer;
    private bool _isStarted;
    private bool _isBusy;

    // The display covered by a fullscreen application, if any, and whether the screen is locked
    private Rect? _fullScreenBounds;
    private volatile bool _isFullScreenOrLocked;
    private bool _isLocked;

    // What's shown now (null until shown on every display), and a sky only shown on the displays
    // that weren't covered (along with the one that was)
    private int? _shownStop;
    private (int Stop, Rect? Skipped)? _partialStop;
    private WallpaperColor? _appliedAccentColor;
    private WallpaperColor? _lastAccentTarget;
    private DateTimeOffset _lastAccentAppliedAt = DateTimeOffset.MinValue;

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
        _dirPath = Path.Combine(
            Path.GetDirectoryName(StartOptions.Current.SettingsPath) ?? AppContext.BaseDirectory,
            "Wallpapers"
        );

        _settingsSubscription = settingsService.WatchProperties(
            [o => o.IsDynamicWallpaperEnabled, o => o.IsAccentColorMatched],
            Evaluate
        );
    }

    public void SetTargetSource(Func<WallpaperTarget?> getTarget) => _getTarget = getTarget;

    public void Start()
    {
        _isStarted = true;
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
                    Interval = CheckInterval,
                };
                _timer.Tick += (_, _) => Check();
                _timer.Start();
            }

            // The accent color was turned off on its own
            if (!IsAccentColorMatched && _appliedAccentColor is not null)
            {
                _appliedAccentColor = null;
                _ = RunLockedAsync(RestoreAccentColor);
            }

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

            // Also puts back what was left over from an earlier run (e.g. after a crash)
            _ = RunLockedAsync(() =>
            {
                RestoreAccentColor();
                RestoreWallpapers();
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
    public void NotifyTargetChanged() => Check();

    // Only looked up when there's something to do, as it opens the foreground application
    private void UpdateVisibility()
    {
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

        _isLocked = InputDesktop.IsLocked();
        _isFullScreenOrLocked = _isLocked || _fullScreenBounds is not null;
    }

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

        // The accent color is still moving (e.g. a slider being dragged), as opposed to the
        // cycle's slow drift
        var isAccentSettled =
            _lastAccentTarget is not { } lastAccent
            || WallpaperColor.GetDifference(lastAccent, accent) <= 1;
        _lastAccentTarget = accent;

        // (a sky already shown on every display that isn't covered isn't shown again until the
        // covered one is uncovered)
        var isSkyNeeded =
            _shownStop != stop
            && !(
                _partialStop is { } partial
                && partial.Stop == stop
                && partial.Skipped == _fullScreenBounds
            );

        var accentDifference = _appliedAccentColor is { } applied
            ? WallpaperColor.GetDifference(applied, accent)
            : int.MaxValue;
        var isAccentNeeded =
            IsAccentColorMatched
            && accentDifference >= NoticeableAccentDifference
            && (isAccentSettled || target.IsImmediate)
            && (
                target.IsImmediate
                // A deliberate change (or the first time), right away
                || accentDifference > 8
                // The cycle moving on, now and then
                || DateTimeOffset.Now - _lastAccentAppliedAt >= AccentDriftInterval
            );

        // Also keeps the background drawing informed, and notices a covered display uncovered
        var isWorking = !_drawing.IsCompleted || _partialStop is not null;
        if (!isSkyNeeded && !isAccentNeeded && !isWorking)
            return;

        UpdateVisibility();

        if (!target.IsImmediate)
        {
            if (_isLocked)
                return;

            if (
                isAccentNeeded
                && _fullScreenBounds is not null
                && DateTimeOffset.Now - _lastAccentAppliedAt < FullScreenAccentInterval
            )
                isAccentNeeded = false;
        }

        // (the covered display is uncovered again)
        if (_partialStop is { } shown && shown.Skipped != _fullScreenBounds)
        {
            _partialStop = null;
            isSkyNeeded = _shownStop != stop || shown.Stop == stop;
        }

        if (isSkyNeeded || isAccentNeeded)
        {
            Apply(
                isSkyNeeded ? stop : null,
                isAccentNeeded ? accent : null,
                target.IsImmediate ? null : _fullScreenBounds
            );
        }
    }

    private async void Apply(int? stop, WallpaperColor? accent, Rect? skippedBounds)
    {
        _isBusy = true;
        var isApplied = false;
        var isSkipped = false;

        try
        {
            await _lock.WaitAsync();
            try
            {
                (isApplied, isSkipped) = await Task.Run(() =>
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

        if (!isApplied)
            return;

        if (stop is not null)
        {
            if (isSkipped)
            {
                _partialStop = (stop.Value, skippedBounds);
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
            _appliedAccentColor = accent;
            _lastAccentAppliedAt = DateTimeOffset.Now;
        }
    }

    private string GetImagePath((int Width, int Height) size, int stop) =>
        Path.Combine(_dirPath, $"{ImagePrefix}{size.Width}x{size.Height} {stop:00}.png");

    // Returns whether it's done, and whether a display was left as it was (as it's covered)
    private (bool IsApplied, bool IsSkipped) ApplyCore(
        int? stop,
        WallpaperColor? accent,
        Rect? skippedBounds
    )
    {
        var isSkipped = false;

        if (stop is { } index)
        {
            using var desktop = DesktopWallpaper.TryCreate();
            if (desktop is null)
                return (false, false);

            var monitors = desktop.GetMonitors();
            if (monitors.Count <= 0)
                return (false, false);

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

                // Covered by a fullscreen application: changed once it's uncovered
                if (bounds == skippedBounds)
                {
                    isSkipped = true;
                    continue;
                }

                var filePath = GetImagePath(size, index);
                if (!File.Exists(filePath))
                {
                    Render(
                        DynamicWallpaper.GetPalette(DynamicWallpaper.GetStops()[index]),
                        size.Item1,
                        size.Item2,
                        filePath,
                        2
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
        }

        if (accent is { } color)
        {
            Directory.CreateDirectory(_dirPath);

            var accentFilePath = Path.Combine(_dirPath, OriginalAccentColorFileName);
            if (!File.Exists(accentFilePath))
                File.WriteAllText(accentFilePath, AccentColor.Save());

            AccentColor.Set(color.Red, color.Green, color.Blue);
        }

        return (true, isSkipped);
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

        _drawing = Task.Run(
            async () =>
            {
                foreach (var (size, stop) in missing)
                {
                    while (_isFullScreenOrLocked)
                        await Task.Delay(FullScreenRetryDelay, token);

                    await _lock.WaitAsync(token);
                    try
                    {
                        var filePath = GetImagePath(size, stop);
                        if (!File.Exists(filePath))
                        {
                            Render(
                                DynamicWallpaper.GetPalette(DynamicWallpaper.GetStops()[stop]),
                                size.Width,
                                size.Height,
                                filePath,
                                1
                            );
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Debug.WriteLine($"Dynamic wallpaper: {ex}");
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }
            },
            token
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

    private bool IsOwnFile(string? filePath) =>
        !string.IsNullOrWhiteSpace(filePath)
        && Path.GetFullPath(filePath)
            .StartsWith(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(_dirPath))
                    + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase
            );

    // Remembers each display's own wallpaper (and how it was fitted), the first time it's
    // replaced, so that it can be put back. Kept in a file, so that it's also put back if
    // Afterglow is turned off after a restart.
    private void RememberOriginalWallpapers(
        DesktopWallpaper desktop,
        IReadOnlyList<(string Id, Rect Bounds)> monitors
    )
    {
        var filePath = Path.Combine(_dirPath, OriginalWallpapersFileName);
        var lines = File.Exists(filePath) ? File.ReadAllLines(filePath).ToList() : [];

        if (!lines.Any(l => l.StartsWith("position\t", StringComparison.Ordinal)))
            lines.Add($"position\t{desktop.Position}");

        var isChanged = false;
        foreach (var (id, _) in monitors)
        {
            if (lines.Any(l => l.StartsWith(id + "\t", StringComparison.OrdinalIgnoreCase)))
                continue;

            var wallpaper = desktop.TryGetWallpaper(id);
            if (wallpaper is null || IsOwnFile(wallpaper))
                continue;

            lines.Add($"{id}\t{wallpaper}");
            isChanged = true;
        }

        if (isChanged || !File.Exists(filePath))
            File.WriteAllLines(filePath, lines);
    }

    private void RestoreWallpapers()
    {
        if (!Directory.Exists(_dirPath))
            return;

        var filePath = Path.Combine(_dirPath, OriginalWallpapersFileName);
        if (File.Exists(filePath))
        {
            using var desktop = DesktopWallpaper.TryCreate();
            if (desktop is null)
                return;

            var connectedIds = desktop
                .GetMonitors()
                .Select(m => m.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

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

                // (displays that aren't connected right now keep what they had)
                if (connectedIds.Contains(parts[0]) && File.Exists(parts[1]))
                    desktop.SetWallpaper(parts[0], parts[1]);
            }

            File.Delete(filePath);
        }

        // The images aren't kept while it's off (they're drawn again when it's turned on)
        foreach (var imagePath in Directory.EnumerateFiles(_dirPath))
        {
            if (
                imagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || imagePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || imagePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
            )
                TryDelete(imagePath);
        }
    }

    private void RestoreAccentColor()
    {
        var filePath = Path.Combine(_dirPath, OriginalAccentColorFileName);
        if (!File.Exists(filePath))
            return;

        AccentColor.Restore(File.ReadAllText(filePath));
        File.Delete(filePath);
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

        Parallel.For(
            0,
            height,
            new ParallelOptions { MaxDegreeOfParallelism = maxThreads },
            () => new byte[width * 4],
            (y, _, row) =>
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
                return row;
            },
            _ => { }
        );

        // Fast compression (smaller files wouldn't be worth the time)
        using var pixmap = bitmap.PeekPixels();
        using var data =
            pixmap.Encode(new SKPngEncoderOptions(SKPngEncoderFilterFlags.Sub, 1))
            ?? throw new InvalidOperationException("Failed to encode the wallpaper.");

        // Written in full before Windows is pointed at it
        var tempFilePath = filePath + ".tmp";
        using (var stream = File.Create(tempFilePath))
            data.SaveTo(stream);

        File.Move(tempFilePath, filePath, true);
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
    }
}
