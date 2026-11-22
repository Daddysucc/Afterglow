using System;
using System.IO;
using System.Reflection;
using Afterglow.Localization;
using Afterglow.PlatformInterop;
using Afterglow.Utils;
using Avalonia;

namespace Afterglow;

public static class Program
{
    private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

    // The assembly name: it names the executable, the settings folder, the startup entry, and the
    // single-instance lock
    public static string Name { get; } = Assembly.GetName().Name ?? "Afterglow";

    // The name Afterglow used for all of the above before it was renamed, which the original
    // LightBulb also uses. Only used to carry existing settings and startup entries over, and to
    // avoid running side by side with an older copy or LightBulb.
    public static string LegacyName { get; } = "LightBulb";

    // The name shown to the user
    public static string DisplayName { get; } = "Afterglow";

    public static Version Version { get; } = Assembly.GetName().Version ?? new Version(0, 0, 0);

    public static string VersionString { get; } = Version.ToString(3);

    public static bool IsDevelopmentBuild { get; } = Version.Major is <= 0 or >= 999;

    public static string ExecutableDirPath { get; } = AppContext.BaseDirectory;

    // Used to start the app with Windows, so it must point to the actual app executable
    public static string ExecutableFilePath { get; } =
        ResolveExecutableFilePath(ExecutableDirPath, Name, Environment.ProcessPath, File.Exists);

    // Assembly locations can't be used for this, because they're empty when published as a single
    // file. The process path can't be relied on either, because it points to the dotnet host when
    // the app is launched through it (e.g. 'dotnet run' or debugging).
    internal static string ResolveExecutableFilePath(
        string baseDirPath,
        string appName,
        string? processPath,
        Func<string, bool> fileExists
    )
    {
        // The app's own executable sits next to it in every deployment mode
        // (regular and single-file publishing, framework-dependent and self-contained, dev builds)
        var appExecutablePath = Path.Combine(baseDirPath, appName + ".exe");
        if (fileExists(appExecutablePath))
            return appExecutablePath;

        if (
            !string.IsNullOrWhiteSpace(processPath)
            && !string.Equals(
                Path.GetFileNameWithoutExtension(processPath),
                "dotnet",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return processPath;
        }

        return appExecutablePath;
    }

    public static string ProjectUrl { get; } = "https://github.com/Daddysucc/Afterglow";

    // Latest stable release, as listed by the GitHub API (drafts and pre-releases are left out)
    public static string LatestReleaseApiUrl { get; } =
        "https://api.github.com/repos/Daddysucc/Afterglow/releases/latest";

    // The running instance can't be asked to show itself, most likely because it's the original
    // LightBulb. Shown before the app starts, so it uses the system language.
    private static void ShowAlreadyRunningMessage()
    {
        var localization = LocalizationManager.GetLocalization(Language.System);

        MessageBox.ShowInfo(
            LocalizationManager.Resolve(
                localization,
                nameof(LocalizationManager.AlreadyRunningTitle)
            ),
            string.Format(
                LocalizationManager.Resolve(
                    localization,
                    nameof(LocalizationManager.AlreadyRunningMessage)
                ),
                DisplayName,
                LegacyName
            )
        );
    }

    public static string ProjectReleasesUrl { get; } = $"{ProjectUrl}/releases";

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .With(
                new Win32PlatformOptions
                {
                    // Use redirection surface composition to avoid Avalonia's WinUI Composition
                    // renderer (WinUiCompositorConnection) from ticking endlessly via dcomp.dll
                    // when the application is idle, which would otherwise wake up dwm.exe
                    // continuously even when monitors are powered off.
                    CompositionMode = [Win32CompositionMode.RedirectionSurface],
                    RenderingMode = [Win32RenderingMode.AngleEgl, Win32RenderingMode.Software],
                }
            )
            .LogToTrace();

    // Lets later launches bring this instance to the front
    internal static SingleInstance? Instance { get; private set; }

    private static Action? _onActivationRequested;

    // Calls the given action (on a thread pool thread) whenever another launch asks this instance
    // to show itself
    internal static void ListenForActivation(Action onActivationRequested)
    {
        _onActivationRequested = onActivationRequested;
        Instance?.ListenForActivation(onActivationRequested);
    }

    // Lets the next version start while this one is still running (an update's installer, or the
    // updated executable), as neither starts while this instance holds on to being the only one
    internal static void ReleaseInstance()
    {
        Instance?.Dispose();
        Instance = null;
    }

    // Holds on to being the only instance again, after an update didn't go ahead
    internal static bool TryReacquireInstance()
    {
        if (Instance is not null)
            return true;

        Instance = SingleInstance.TryAcquire(Name);
        if (Instance is null)
            return false;

        if (_onActivationRequested is not null)
            Instance.ListenForActivation(_onActivationRequested);

        return true;
    }

    // After a portable copy has been updated, the previous version starts this one and closes.
    // Waiting for it makes sure that it can't reset the screen colors after this one has set them.
    private static void WaitForPreviousVersionToExit(int processId)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);

            // Only if it's still this app (the process ID may have been reused since)
            using var current = System.Diagnostics.Process.GetCurrentProcess();
            if (
                string.Equals(
                    process.ProcessName,
                    current.ProcessName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                process.WaitForExit(TimeSpan.FromSeconds(30));
            }
        }
        // It has already exited
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { }
    }

    [STAThread]
    public static int Main(string[] args)
    {
        if (StartOptions.Current.PreviousVersionProcessId is { } previousVersionProcessId)
            WaitForPreviousVersionToExit(previousVersionProcessId);

        // Ensure only one instance of the app is running at a time (the installer also checks it)
        Instance = SingleInstance.TryAcquire(Name);
        if (Instance is null)
        {
            // Show the running instance, rather than seemingly doing nothing (it may well be
            // hidden in the system tray)
            if (!SingleInstance.TryActivateRunningInstance(Name))
                ShowAlreadyRunningMessage();

            return 1;
        }

        try
        {
            // A copy from before the rename, or the original LightBulb, uses the legacy name. Both
            // adjust the screen colors, so they can't run side by side with this one.
            if (SingleInstance.IsRunning(LegacyName))
            {
                if (!SingleInstance.TryActivateRunningInstance(LegacyName))
                    ShowAlreadyRunningMessage();

                return 1;
            }

            // Carry over the settings and startup entry of a copy from before the rename
            LegacyMigration.Run(StartOptions.Current);

            // Remove what's left over from updating to this version
            UpdateHandoff.CleanUp();

            // Build and run the app
            var builder = BuildAvaloniaApp();

            try
            {
                return builder.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                MessageBox.ShowError("Fatal Error", ex.ToString());
                throw;
            }
            finally
            {
                // Clean up after application shutdown
                if (builder.Instance is IDisposable disposableApp)
                    disposableApp.Dispose();
            }
        }
        finally
        {
            ReleaseInstance();
        }
    }
}
