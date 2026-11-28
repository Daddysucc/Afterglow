using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Afterglow.Core;
using Afterglow.Core.Utils;
using Afterglow.PlatformInterop;
using Avalonia.Threading;
using PowerKit.Extensions;
using PowerKit.Json.Extensions;

namespace Afterglow.Services;

/// <summary>
/// Keeps a forecast of the weather where the user is, for the dynamic wallpaper's sky, while
/// that's turned on (see <see cref="SettingsService.IsSkyWeatherEnabled" />).
/// </summary>
/// <remarks>
/// <para>
/// One request returns the next few hours in 15-minute slots (see <see cref="WeatherForecast"
/// />), and the weather models only update every hour or few, so it's fetched rarely. It doesn't
/// wake up in between, and while the screen is locked it waits for the user to be back.
/// </para>
/// <para>
/// It makes do with whatever it has, whatever goes wrong:
/// <list type="bullet">
/// <item>Some values not being available where the user is, or the air quality not coming
/// through, is made up for in the forecast itself (see <see cref="WeatherForecast" />).</item>
/// <item>The forecast is kept in a small file next to the settings, so that it's there right away
/// after a restart, even while offline.</item>
/// <item>If it can't be fetched (offline, or the service is down), it's tried again in a while
/// (less and less often), and right away once Windows says the network is back. Meanwhile, the
/// forecast it has keeps going, and once it runs out, the weather it ended with fades away
/// slowly (as the weather tends to stay as it was for a while), to a clear sky.</item>
/// <item>While a fresh forecast is on its way (when the app starts or the computer wakes up, with
/// one that isn't fresh, or none, e.g. before the network is up), the sky holds still for up to a
/// couple of minutes (see <see cref="IsWaiting" />), rather than heading for an old (or clear)
/// sky only to turn back.</item>
/// </list>
/// </para>
/// <para>
/// When the app starts or the computer wakes up, the forecast is fetched again unless it's fresh,
/// and when the user turns the weather on or off (or moves), it's fetched (or dropped) right away,
/// and shown right away (see <see cref="ShowRequested" />).
/// </para>
/// </remarks>
public class WeatherService : IDisposable
{
    // (as often as the fastest weather models are updated)
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);

    // (twice as often while rain, snow or a storm is about, as it changes quickly then, and the
    // models catch up with it as it comes)
    private static readonly TimeSpan UnsettledRefreshInterval = TimeSpan.FromMinutes(30);

    private TimeSpan GetRefreshInterval() =>
        _forecast?.IsUnsettled(DateTimeOffset.Now) == true
            ? UnsettledRefreshInterval
            : RefreshInterval;

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

    // After a failure, tried again in a while, waiting twice as long after each one in a row (up
    // to the regular pace), so that being offline (or the service being down) costs next to nothing
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(15);

    // (not more often than this, however often the network comes and goes)
    private static readonly TimeSpan MinRetryInterval = TimeSpan.FromMinutes(1);

    // How long the weather a forecast ended with takes to fade away
    private static readonly TimeSpan FadeTime = TimeSpan.FromHours(6);

    // How long the sky holds still while there's no forecast yet
    private static readonly TimeSpan WaitTime = TimeSpan.FromMinutes(2);

    // A forecast fetched longer ago than this is fetched again when the app starts, or the
    // computer wakes up (so that the sky is as up to date as it can be then)
    private static readonly TimeSpan FreshTime = TimeSpan.FromMinutes(15);

    private readonly SettingsService _settingsService;
    private readonly IDisposable _settingsSubscription;
    private readonly string _filePath;
    private IDisposable? _sessionSubscription;

    private DispatcherTimer? _timer;
    private bool _isStarted;
    private bool _isFetching;
    private int _failureCount;
    private DateTimeOffset _nextFetch = DateTimeOffset.MinValue;
    private DateTimeOffset _lastFetch = DateTimeOffset.MinValue;
    private DateTimeOffset? _waitUntil;

    // The forecast, and the request it came from (a new location starts over)
    private WeatherForecast? _forecast;
    private string? _forecastUrl;

    // How high up the location is: the ground doesn't move, so this is kept for as long as the
    // location is the same, even when the forecast runs out (see GetAt)
    private double _altitude;

    // When the forecast was last fetched, and whether the user just turned the weather on (or
    // moved), so that it's shown right away once it's there
    private DateTimeOffset _fetchedAt = DateTimeOffset.MinValue;
    private bool _isShowRequested;
    private bool _isEvaluated;

    /// <summary>
    /// The user turned the weather on or off (or moved), and it's there now (or gone): the sky
    /// should show it right away.
    /// </summary>
    public event EventHandler? ShowRequested;

    public WeatherService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _filePath = Path.Combine(
            Path.GetDirectoryName(StartOptions.Current.SettingsPath) ?? AppContext.BaseDirectory,
            "Weather forecast.json"
        );

        _settingsSubscription = settingsService.WatchProperties(
            [o => o.IsDynamicWallpaperEnabled, o => o.IsSkyWeatherEnabled, o => o.Location],
            Evaluate
        );
    }

    private string? Url =>
        _isStarted
        && _settingsService
            is {
                IsDynamicWallpaperEnabled: true,
                IsSkyWeatherEnabled: true,
                Location: { IsValid: true } location,
            }
            ? WeatherForecast.GetUrl(location)
            : null;

    private string? AirQualityUrl =>
        _settingsService.Location is { IsValid: true } location
            ? WeatherForecast.GetAirQualityUrl(location)
            : null;

    /// <summary>
    /// Whether the weather isn't known yet, but should be shortly (the first fetch hasn't been
    /// tried yet), so the sky should hold still for now.
    /// </summary>
    public bool IsWaiting =>
        Url is not null && _waitUntil is { } until && DateTimeOffset.Now < until;

    /// <summary>
    /// Gets the weather at the given time, if it's turned on and there's a forecast for it.
    /// </summary>
    /// <remarks>
    /// Once the forecast runs out (e.g. while offline), the weather it ended with fades away, to a
    /// clear sky (but still as it is as high up as the user is).
    /// </remarks>
    public SkyConditions? GetAt(DateTimeOffset instant) =>
        Url is null ? null
        : _forecast?.GetAt(instant, FadeTime) is { } conditions
            ? conditions with
            {
                Altitude = _altitude,
            }
        : _altitude > 0 ? SkyConditions.Clear with { Altitude = _altitude }
        : null;

    public void Start()
    {
        _isStarted = true;

        // Back at the computer, or the network back: fetched if it's been waiting for that
        _sessionSubscription = PowerKit.Disposable.Merge(
            SessionEvents.RegisterLock(isLocked =>
            {
                if (!isLocked)
                    Dispatcher.UIThread.Post(Check);
            }),
            SessionEvents.RegisterSleep(isSuspending =>
            {
                if (!isSuspending)
                    Dispatcher.UIThread.Post(OnWokenUp);
            })
        );
        TryLoad();
        Evaluate();
    }

    // Woken up: the forecast is fetched again if it isn't fresh (and the sky goes straight to it,
    // see WallpaperService)
    private void OnWokenUp()
    {
        if (Url is not null && DateTimeOffset.Now - _fetchedAt >= FreshTime)
        {
            _nextFetch = DateTimeOffset.MinValue;
            _waitUntil = DateTimeOffset.Now + WaitTime;
        }

        Check();
    }

    // (told on a thread of its own)
    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs args)
    {
        if (!args.IsAvailable)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            // Only if it's been failing (or has nothing), and not over and over (unless the sky is
            // waiting for it)
            if (
                Url is null
                || (_failureCount == 0 && _forecast is not null && !IsWaiting)
                || (!IsWaiting && DateTimeOffset.Now - _lastFetch < MinRetryInterval)
            )
                return;

            _nextFetch = DateTimeOffset.MinValue;
            Check();
        });
    }

    private void Evaluate()
    {
        if (!_isStarted)
            return;

        var url = Url;
        if (url != _forecastUrl)
        {
            // Turned on or off, or somewhere else now, by the user (rather than as the app starts):
            // shown right away, once it's there
            if (_isEvaluated)
                _isShowRequested = true;

            // Turned off, or somewhere else now (and waiting for the weather there)
            _forecast = null;
            _altitude = 0;
            _forecastUrl = null;
            _nextFetch = DateTimeOffset.MinValue;
            _failureCount = 0;
            _waitUntil = url is not null ? DateTimeOffset.Now + WaitTime : null;
            TryDelete();
        }

        _isEvaluated = true;

        // (Windows only tells about the network coming back while it's needed)
        SetNetworkListened(url is not null);

        if (url is null)
        {
            _timer?.Stop();
            _timer = null;
            TryDelete();

            // (turned off: the clear sky right away)
            if (_isShowRequested)
            {
                _isShowRequested = false;
                ShowRequested?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        Check();
    }

    private bool _isNetworkListened;

    private void SetNetworkListened(bool isListened)
    {
        if (isListened == _isNetworkListened)
            return;

        if (isListened)
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        else
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;

        _isNetworkListened = isListened;
    }

    private void Check()
    {
        if (Url is not { } url || _isFetching)
            return;

        var now = DateTimeOffset.Now;
        if (now < _nextFetch)
        {
            Schedule(_nextFetch - now);
            return;
        }

        // While the screen is locked, nobody sees the sky: fetched once the user is back (or at
        // the regular pace, if that comes first)
        if (InputDesktop.IsLocked())
        {
            Schedule(RefreshInterval);
            return;
        }

        _ = FetchAsync(url);
    }

    // A single timer, only for the next fetch (rather than a regular tick)
    private void Schedule(TimeSpan delay)
    {
        if (_timer is null)
        {
            _timer = new DispatcherTimer(DispatcherPriority.Background);
            _timer.Tick += (_, _) =>
            {
                _timer?.Stop();
                Check();
            };
        }

        _timer.Stop();
        _timer.Interval = delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1);
        _timer.Start();
    }

    // Gets the response, or null if there isn't a usable one (whatever went wrong)
    private static async Task<JsonElement?> TryGetJsonAsync(string? url)
    {
        if (url is null)
            return null;

        try
        {
            using var cancellation = new CancellationTokenSource(RequestTimeout);
            var json = await Http.Client.GetJsonAsync(url, cancellation.Token);

            // (an error, e.g. a location the service doesn't cover, comes back as its reason)
            return json.ValueKind == JsonValueKind.Object && !json.TryGetProperty("error", out _)
                ? json
                : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Weather: {ex}");
            return null;
        }
    }

    private async Task FetchAsync(string url)
    {
        _isFetching = true;
        _lastFetch = DateTimeOffset.Now;
        var isUpdated = false;

        try
        {
            // The weather and the air come from separate services, so either can fail on its own
            var weatherTask = TryGetJsonAsync(url);
            var airTask = TryGetJsonAsync(AirQualityUrl);
            var (weather, air) = (await weatherTask, await airTask);

            var forecast = WeatherForecast.Parse(weather, air);
            var isWeatherKnown =
                weather is { } w
                && (w.TryGetProperty("minutely_15", out _) || w.TryGetProperty("hourly", out _));

            // A forecast without the weather (only the air) is only better than nothing, so it
            // doesn't replace one that has it while that's still good
            var isUsable =
                forecast.Slots.Count > 0
                && (isWeatherKnown || _forecast?.GetAt(DateTimeOffset.Now) is null);

            // (unless it was turned off, or moved, meanwhile)
            if (isUsable && Url == url)
            {
                _forecast = forecast;
                if (forecast.Altitude > 0 || isWeatherKnown)
                    _altitude = forecast.Altitude;
                _forecastUrl = url;
                _fetchedAt = DateTimeOffset.Now;
                isUpdated = true;
                Save(forecast, url);
            }

            _failureCount = isWeatherKnown && air is not null ? 0 : _failureCount + 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Weather: {ex}");
            _failureCount++;
        }
        finally
        {
            _isFetching = false;
        }

        // The weather is there (the sky stops waiting, and if the user just turned it on, shows it
        // right away), or, if it couldn't be fetched, it's tried again (also as soon as the
        // network is back), while the sky waits a little longer for it
        if (isUpdated || !IsWaiting)
        {
            // (only while the sky is still waiting for it: not out of the blue, much later)
            var isShown = isUpdated && _isShowRequested && IsWaiting && Url == url;
            _waitUntil = null;
            _isShowRequested = false;

            if (isShown)
                ShowRequested?.Invoke(this, EventArgs.Empty);
        }

        // (offline, or the service is down: the forecast it has keeps going meanwhile)
        _forecastUrl ??= Url == url ? url : null;
        _nextFetch =
            DateTimeOffset.Now
            + (
                _failureCount == 0
                    ? GetRefreshInterval()
                    : TimeSpan.FromTicks(
                        Math.Min(
                            RetryInterval.Ticks * (1L << Math.Min(_failureCount - 1, 4)),
                            RefreshInterval.Ticks
                        )
                    )
            );

        Evaluate();
    }

    // Takes in the forecast kept from before (if it's for the same place), and fetches it again
    // when it's due, rather than right away
    private void TryLoad()
    {
        if (Url is not { } url || !File.Exists(_filePath))
            return;

        try
        {
            if (
                WeatherForecast.TryRead(File.ReadAllText(_filePath))
                    is not var (forecast, source, fetchedAt)
                || source != url
            )
                return;

            _forecast = forecast;
            _forecastUrl = url;
            _altitude = forecast.Altitude;
            _fetchedAt = fetchedAt;

            // (fetched again right away, unless it's fresh, and the sky waits a moment for that)
            var isFresh = DateTimeOffset.Now - fetchedAt < FreshTime;
            _nextFetch = isFresh ? fetchedAt + GetRefreshInterval() : DateTimeOffset.MinValue;
            if (!isFresh)
                _waitUntil = DateTimeOffset.Now + WaitTime;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Weather: {ex}");
        }
    }

    // (in the background, as a whole file at once, so that it's never left half-written)
    private void Save(WeatherForecast forecast, string url)
    {
        var json = forecast.Write(url, DateTimeOffset.Now);
        var filePath = _filePath;

        _ = Task.Run(() =>
        {
            var tempFilePath = filePath + ".tmp";
            try
            {
                File.WriteAllText(tempFilePath, json);
                File.Move(tempFilePath, filePath, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Weather: {ex}");
            }
        });
    }

    // (turned off, or somewhere else now: the forecast isn't kept)
    private void TryDelete()
    {
        try
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Weather: {ex}");
        }
    }

    public void Dispose()
    {
        SetNetworkListened(false);
        _settingsSubscription.Dispose();
        _sessionSubscription?.Dispose();
        _timer?.Stop();
    }
}
