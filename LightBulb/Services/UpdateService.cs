using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LightBulb.Core;
using PowerKit.Extensions;
using Timer = LightBulb.PlatformInterop.Timer;

namespace LightBulb.Services;

/// <summary>
/// Checks GitHub for a newer release every now and then. Only the latest version number is looked
/// up: nothing is downloaded or installed, the release page is just opened when asked.
/// </summary>
public partial class UpdateService : ObservableObject, IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly Timer _timer;
    private readonly IDisposable _settingsSubscription;

    // Only set once a check succeeds, so that failed checks (e.g. no connection yet) are retried
    private DateTimeOffset? _lastCheckedAt;
    private int _isChecking;
    private bool _isStarted;

    public UpdateService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        // Not right away: the connection may not be up yet when starting with Windows. After that,
        // every half an hour, which only checks again if the last check was a while ago.
        _timer = new Timer(
            TimeSpan.FromSeconds(15),
            TimeSpan.FromMinutes(30),
            () => _ = CheckIfDueAsync()
        );

        _settingsSubscription = settingsService.WatchProperty(
            o => o.IsUpdateCheckEnabled,
            isEnabled =>
            {
                // Checked again as soon as it's turned back on
                _lastCheckedAt = null;

                if (isEnabled)
                    _ = CheckIfDueAsync();
                else
                    AvailableVersion = null;
            }
        );
    }

    // Development builds aren't released, so they're never compared against releases
    public static bool IsSupported => !Program.IsDevelopmentBuild;

    // Newer released version, if there is one
    [ObservableProperty]
    public partial Version? AvailableVersion { get; private set; }

    public void Start()
    {
        if (!IsSupported)
            return;

        _isStarted = true;
        _timer.Start();
    }

    private async Task CheckIfDueAsync()
    {
        if (!_isStarted || !_settingsService.IsUpdateCheckEnabled)
            return;

        if (!UpdateCheck.IsDue(_lastCheckedAt, DateTimeOffset.Now))
            return;

        // One check at a time
        if (Interlocked.Exchange(ref _isChecking, 1) != 0)
            return;

        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var version = await UpdateCheck.GetNewerVersionAsync(
                Program.LatestReleaseApiUrl,
                Program.Version,
                cancellation.Token
            );

            _lastCheckedAt = DateTimeOffset.Now;

            Dispatcher.UIThread.Post(() =>
                AvailableVersion = _settingsService.IsUpdateCheckEnabled ? version : null
            );
        }
        // Not being able to check (e.g. offline, or GitHub is unavailable) is never an error
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check for updates: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _isChecking, 0);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _settingsSubscription.Dispose();
    }
}
