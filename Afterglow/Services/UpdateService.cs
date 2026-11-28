using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Afterglow.Core;
using Afterglow.Core.Utils;
using Afterglow.Utils;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using PowerKit;
using PowerKit.Extensions;
using Timer = Afterglow.PlatformInterop.Timer;

namespace Afterglow.Services;

/// <summary>
/// Checks GitHub for a newer release every now and then, and updates to it when asked: nothing is
/// downloaded or installed until then.
/// </summary>
public partial class UpdateService : ObservableObject, IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly Timer _timer;
    private readonly IDisposable _settingsSubscription;

    private int _isChecking;
    private bool _isStarted;

    // Whether a check has succeeded since the app started
    private bool _hasCheckedSinceStart;

    public UpdateService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        // Not right away: the connection may not be up yet when starting with Windows. After that,
        // every half an hour, which only checks once the chosen time has passed since the last
        // successful check (which is remembered between launches, so failed checks are retried).
        _timer = new Timer(
            TimeSpan.FromSeconds(15),
            TimeSpan.FromMinutes(30),
            () => Dispatcher.UIThread.Post(() => _ = CheckIfDueAsync())
        );

        _settingsSubscription = Disposable.Merge(
            settingsService.WatchProperty(
                o => o.IsUpdateCheckEnabled,
                isEnabled =>
                {
                    // Checked as soon as it's turned (back) on
                    if (isEnabled)
                        _ = CheckIfDueAsync(true);
                    else
                    {
                        AvailableVersion = null;
                        Status = UpdateCheckStatus.None;
                    }
                }
            ),
            // Checked right away if that makes a check due (e.g. after switching to daily checks)
            settingsService.WatchProperty(
                o => o.UpdateCheckFrequency,
                frequency => _ = CheckIfDueAsync()
            )
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

    // Outcome of the last check, shown next to the button that checks on request
    [ObservableProperty]
    public partial UpdateCheckStatus Status { get; private set; }

    // Must be called on the UI thread, like everything that reads or changes the settings
    private async Task CheckIfDueAsync(bool isForced = false)
    {
        if (!_isStarted || !_settingsService.IsUpdateCheckEnabled)
            return;

        var isDue =
            isForced
            || UpdateCheck.IsDue(
                _settingsService.LastUpdateCheckTime,
                DateTimeOffset.Now,
                UpdateCheck.GetInterval(_settingsService.UpdateCheckFrequency)
            )
            // An update found before the app was restarted is offered again right away, rather
            // than only after the next scheduled check
            || (
                !_hasCheckedSinceStart
                && UpdateCheck.IsKnownUpdatePending(
                    _settingsService.AvailableUpdateVersion,
                    Program.Version
                )
            );

        if (isDue)
            await CheckAsync();
    }

    /// <summary>
    /// Checks right away, when asked to: even if checking now and then is turned off.
    /// </summary>
    public async Task CheckNowAsync()
    {
        if (IsSupported)
            await CheckAsync();
    }

    private async Task CheckAsync()
    {
        // One check at a time
        if (Interlocked.Exchange(ref _isChecking, 1) != 0)
            return;

        Dispatcher.UIThread.Post(() => Status = UpdateCheckStatus.Checking);

        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var release = await UpdateCheck.GetNewerReleaseAsync(
                Program.LatestReleaseApiUrl,
                Program.Version,
                cancellation.Token
            );

            var checkedAt = DateTimeOffset.Now;

            Dispatcher.UIThread.Post(() =>
            {
                // Remembered for the next launch (as shown in the settings, too)
                _hasCheckedSinceStart = true;
                _settingsService.LastUpdateCheckTime = checkedAt;
                _settingsService.AvailableUpdateVersion = release?.Version.ToString(3);
                _settingsService.TrySaveToFile();

                // A newer release may have come out since an update failed, so try again
                if (
                    release?.Version != AvailableVersion
                    && InstallState == UpdateInstallState.Failed
                )
                    InstallState = UpdateInstallState.None;

                // What's changed is fetched right away, so that it's ready to be shown
                if (release?.Version != _availableRelease?.Version)
                    _changes = release is not null ? FetchChangesAsync(release) : null;

                _availableRelease = release;
                AvailableVersion = release?.Version;
                Status = release is not null
                    ? UpdateCheckStatus.UpdateAvailable
                    : UpdateCheckStatus.UpToDate;
            });
        }
        // Not being able to check (e.g. offline, or GitHub is unavailable) is never an error
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check for updates: {ex.Message}");
            Dispatcher.UIThread.Post(() => Status = UpdateCheckStatus.Failed);
        }
        finally
        {
            Interlocked.Exchange(ref _isChecking, 0);
        }
    }

    // ---- Installing ----

    private Release? _availableRelease;

    // What's changed in the available update (as plain text), if its release says so
    private string? AvailableReleaseNotes =>
        _availableRelease?.Notes is { } notes
        && ReleaseNotes.ToPlainText(notes) is { Length: > 0 } text
            ? text
            : null;

    /// <summary>
    /// What's changed since this version, up to the available update: every version in between,
    /// as the changelog says (each under a heading in the given format, e.g. "New in {0}"), or
    /// else what the update's release says, or null if neither says.
    /// </summary>
    public async Task<string?> GetChangesAsync(string headingFormat)
    {
        if (_availableRelease is null)
            return null;

        var changelog = _changes is not null ? await _changes : null;
        if (changelog is not null && _availableRelease is { } release)
        {
            var changes = ReleaseNotes.GetChangesBetween(
                changelog,
                Program.Version,
                release.Version,
                headingFormat
            );

            if (changes is not null)
                return changes;
        }

        return AvailableReleaseNotes;
    }

    // The changelog as of the available update's release (or null if it couldn't be fetched)
    private Task<string?>? _changes;

    private static async Task<string?> FetchChangesAsync(Release release)
    {
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            return await Http.Client.GetStringAsync(
                Program.GetChangelogUrl(release.Version),
                cancellation.Token
            );
        }
        // (offline, or the file isn't there, e.g. for releases from before it was kept)
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get the changelog: {ex.Message}");
            return null;
        }
    }

    // How far updating has come, shown on the update buttons
    [ObservableProperty]
    public partial UpdateInstallState InstallState { get; private set; }

    // Fraction of the update that has been downloaded
    [ObservableProperty]
    public partial double DownloadProgress { get; private set; }

    // Installed copies are updated by the installer, portable ones replace their own executable
    private static bool IsInstalled =>
        File.Exists(Path.Combine(Program.ExecutableDirPath, ".installed"));

    /// <summary>
    /// Downloads and installs the available update, then closes the app (the new version starts
    /// once it's installed). If it can't be installed automatically, the download page is opened
    /// the next time this is called. Must be called on the UI thread.
    /// </summary>
    public async Task UpdateAsync()
    {
        switch (InstallState)
        {
            case UpdateInstallState.Downloading or UpdateInstallState.Installing:
                return;

            case UpdateInstallState.Failed:
                OpenReleasePage();
                return;
        }

        if (_availableRelease is not { } release)
            return;

        InstallState = UpdateInstallState.Downloading;
        DownloadProgress = 0;

        try
        {
            var isStarted = IsInstalled
                ? await InstallWithInstallerAsync(release)
                : await ReplaceExecutableAsync(release);

            if (isStarted)
            {
                // The new version takes over from here
                App.Shutdown();
                return;
            }

            // Declined at the administrator prompt, so nothing has changed
            InstallState = UpdateInstallState.None;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to update: {ex}");
            InstallState = UpdateInstallState.Failed;
        }
    }

    private static void OpenReleasePage() =>
        System.Diagnostics.Process.StartShellExecute($"{Program.ProjectReleasesUrl}/latest");

    private ReleaseAsset GetAsset(Release release, bool isInstaller)
    {
        var assetName = UpdatePackage.TryGetAssetName(
            Program.Name,
            isInstaller,
            RuntimeInformation.ProcessArchitecture
        );

        var asset = assetName is not null ? UpdatePackage.TryFindAsset(release, assetName) : null;

        return asset is not null && UpdatePackage.CanDownload(asset)
            ? asset
            : throw new InvalidOperationException(
                $"Release {release.Version} has no verifiable '{assetName}' to update with."
            );
    }

    private Progress<double> CreateDownloadProgress() => new(p => DownloadProgress = p);

    private async Task<bool> InstallWithInstallerAsync(Release release)
    {
        var asset = GetAsset(release, true);

        Directory.CreateDirectory(UpdateHandoff.InstallerDirPath);
        var installerPath = Path.Combine(UpdateHandoff.InstallerDirPath, asset.Name);

        using (var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(15)))
        {
            await UpdatePackage.DownloadAsync(
                asset,
                installerPath,
                CreateDownloadProgress(),
                cancellation.Token
            );
        }

        InstallState = UpdateInstallState.Installing;
        return await UpdateHandoff.StartInstallerAsync(installerPath);
    }

    private async Task<bool> ReplaceExecutableAsync(Release release)
    {
        var asset = GetAsset(release, false);

        var executablePath =
            UpdateHandoff.TryGetRunningExecutablePath()
            ?? throw new InvalidOperationException("The app's own executable can't be replaced.");

        // Downloaded next to it, so that it can be moved in place without copying
        var newExecutablePath = UpdateHandoff.GetNewExecutablePath(executablePath);

        using (var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(15)))
        {
            await UpdatePackage.DownloadAsync(
                asset,
                newExecutablePath,
                CreateDownloadProgress(),
                cancellation.Token
            );
        }

        InstallState = UpdateInstallState.Installing;
        UpdateHandoff.ReplaceExecutable(executablePath, newExecutablePath);

        return true;
    }

    public void Dispose()
    {
        _timer.Dispose();
        _settingsSubscription.Dispose();
    }
}

public enum UpdateInstallState
{
    // Not updating (or declined at the administrator prompt)
    None,
    Downloading,
    Installing,

    // Couldn't update automatically, so the download page is offered instead
    Failed,
}

public enum UpdateCheckStatus
{
    // Not checked yet
    None,
    Checking,
    UpToDate,
    UpdateAvailable,
    Failed,
}
