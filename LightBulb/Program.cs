using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Avalonia;
using LightBulb.PlatformInterop;

namespace LightBulb;

public static class Program
{
    private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

    // The assembly name, which stays "LightBulb" for compatibility: it names the executable,
    // the settings folder, the startup entry, and the single-instance lock, which existing
    // installations and settings rely on
    public static string Name { get; } = Assembly.GetName().Name ?? "LightBulb";

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

    [STAThread]
    public static int Main(string[] args)
    {
        // Ensure only one instance of the app is running at a time
        using var identityMutex = new Mutex(
            true,
            $"{Name}_Identity",
            out var isOnlyRunningInstance
        );

        if (!isOnlyRunningInstance)
            return 1;

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
}
