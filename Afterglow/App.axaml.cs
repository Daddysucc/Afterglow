using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Afterglow.Framework;
using Afterglow.Localization;
using Afterglow.PlatformInterop;
using Afterglow.Services;
using Afterglow.Utils.Extensions;
using Afterglow.ViewModels;
using Afterglow.ViewModels.Components;
using Afterglow.ViewModels.Components.Settings;
using Afterglow.ViewModels.Dialogs;
using Afterglow.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Material.Styles.Themes;
using Microsoft.Extensions.DependencyInjection;
using PowerKit.Extensions;

namespace Afterglow;

public partial class App : Application, IDisposable
{
    private readonly ServiceProvider _services;
    private readonly SettingsService _settingsService;
    private readonly MainViewModel _mainViewModel;

    private NotifyIcon? _notifyIcon;
    private IDisposable? _notifyIconSubscription;
    private IDisposable? _hueSubscription;
    private TrayMenuWindow? _trayMenu;
    private int _isDisposed;

    public App()
    {
        var services = new ServiceCollection();

        // Framework
        services.AddSingleton<DialogManager>();
        services.AddSingleton<ViewManager>();
        services.AddSingleton<ViewModelManager>();

        // Services
        services.AddSingleton<ExternalApplicationService>();
        services.AddSingleton<LocalizationManager>();
        services.AddSingleton<MonitorService>();
        services.AddSingleton<GammaService>();
        services.AddSingleton<GrayscaleService>();
        services.AddSingleton<HotKeyService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<HueService>();
        services.AddSingleton<WallpaperService>();

        // View models
        services.AddTransient<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddTransient<MessageBoxViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsTabViewModelBase, CycleSettingsTabViewModel>();
        services.AddTransient<SettingsTabViewModelBase, ScheduleSettingsTabViewModel>();
        services.AddTransient<SettingsTabViewModelBase, RulesSettingsTabViewModel>();
        services.AddTransient<SettingsTabViewModelBase, ControlsSettingsTabViewModel>();
        services.AddTransient<SettingsTabViewModelBase, SmartLightsSettingsTabViewModel>();

        _services = services.BuildServiceProvider(true);
        _settingsService = _services.GetRequiredService<SettingsService>();
        _mainViewModel = _services.GetRequiredService<ViewModelManager>().GetMainViewModel();
    }

    public override void Initialize()
    {
        base.Initialize();

        AvaloniaXamlLoader.Load(this);

        DataContext = _mainViewModel;
    }

    // The whole application uses a single dark-slate palette, applied on top of the Material theme
    // so that standard controls (text boxes, drop-downs, scroll bars, sliders, etc.) match it too
    private void InitializeTheme()
    {
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;

        var accent = Color.Parse("#6C8CD5");
        var theme = Theme.Create(Theme.Dark, accent, accent);

        theme.Background = Color.Parse("#22262A");
        theme.Paper = Color.Parse("#22262A");
        theme.CardBackground = Color.Parse("#2B3037");
        theme.ToolBarBackground = Color.Parse("#1B1E22");
        theme.Body = Color.Parse("#F0F2F5");
        theme.BodyLight = Color.Parse("#9AA4B2");
        theme.Divider = Color.Parse("#383F48");
        theme.Selection = Color.Parse("#3A4660");
        theme.CheckBoxOff = Color.Parse("#9AA4B2");
        theme.CheckBoxDisabled = Color.Parse("#4A525D");
        theme.ToolTipBackground = Color.Parse("#1E2226");
        theme.ChipBackground = Color.Parse("#383F48");
        theme.TextBoxBorder = Color.Parse("#4A525D");
        theme.TextAreaBorder = Color.Parse("#4A525D");
        theme.TextAreaInactiveBorder = Color.Parse("#383F48");
        theme.TextFieldBoxBackground = Color.Parse("#2B3037");
        theme.TextFieldBoxHoverBackground = Color.Parse("#323840");
        theme.TextFieldBoxDisabledBackground = Color.Parse("#262A2F");
        theme.FlatButtonClick = Color.Parse("#3A414B");
        theme.FlatButtonRipple = Color.Parse("#4A525D");

        this.LocateMaterialTheme<MaterialThemeBase>().CurrentTheme = theme;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Load settings
        _settingsService.Load();

        // Make Hue lights follow the cycle as the screen shows it, if that's turned on (only
        // while Afterglow is on, or the cycle preview is playing)
        var hueService = _services.GetRequiredService<HueService>();
        var dashboard = _mainViewModel.Dashboard;
        hueService.SetCycleEnabled(dashboard.IsEnabled);
        hueService.SetTargetSource(() =>
            new HueTarget(
                dashboard.LightConfiguration,
                dashboard.IsScreenPausedAutomatically,
                dashboard.IsCyclePreviewEnabled
            )
        );

        // Draw the desktop wallpaper as the sky, and set the accent color to the screen's colors
        // (neutral while Afterglow is off or paused), if that's turned on, but not while the
        // cycle preview plays through the day (other than when stepping through it)
        var wallpaperService = _services.GetRequiredService<WallpaperService>();
        wallpaperService.SetTargetSource(() =>
            dashboard.IsCyclePreviewEnabled && !dashboard.IsSteppingThroughDay
                ? null
                : new WallpaperTarget(
                    dashboard.IsActive
                        ? dashboard.LightConfiguration
                        : dashboard.TargetConfiguration,
                    dashboard.SunElevation,
                    dashboard.IsSteppingThroughDay
                )
        );

        void OnDashboardChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(DashboardViewModel.IsEnabled):
                    hueService.SetCycleEnabled(dashboard.IsEnabled);
                    break;

                // Adjusted by the user (with hotkeys), rather than by the cycle moving on
                // Stepping through the day (to try out the dynamic wallpaper)
                case nameof(DashboardViewModel.Instant) when dashboard.IsSteppingThroughDay:
                    hueService.NotifyTargetChanged();
                    wallpaperService.NotifyTargetChanged();
                    break;

                case nameof(DashboardViewModel.TemperatureOffset)
                or nameof(DashboardViewModel.BrightnessOffset):
                    hueService.NotifyUserChange();
                    hueService.NotifyTargetChanged();
                    wallpaperService.NotifyTargetChanged();
                    break;

                // Whatever changes what the screen shows (the instant moves on every update)
                case nameof(DashboardViewModel.TargetConfiguration)
                or nameof(DashboardViewModel.IsCyclePreviewEnabled)
                or nameof(DashboardViewModel.IsPaused)
                or nameof(DashboardViewModel.PausedMonitors):
                    hueService.NotifyTargetChanged();
                    break;
            }
        }

        dashboard.PropertyChanged += OnDashboardChanged;
        _hueSubscription = PowerKit.Disposable.Create(() =>
            dashboard.PropertyChanged -= OnDashboardChanged
        );
        hueService.Start();
        wallpaperService.Start();

        // The cycle runs whether or not the window is ever shown
        _ = dashboard.InitializeAsync();

        // Initialize the lifetime
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The window may not exist (see ShowMainWindow), so the app keeps running without
            // one, until it's told to exit
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Started hidden, the window isn't even created, unless there's something to tell
            // the user right away (e.g. the welcome message)
            if (!_mainViewModel.IsStartHidden || _mainViewModel.HasStartupMessages)
                ShowMainWindow();

            // Although `App.Dispose()` is invoked from `Program.Main(...)`, on some platforms
            // it may be called too late in the shutdown lifecycle. Attach an exit
            // handler to ensure timely disposal as a safeguard.
            // https://github.com/Tyrrrz/YoutubeDownloader/issues/795
            desktop.Exit += (_, _) => Dispose();

            // Save pending changes and remove the tray icon even if the app goes down abruptly
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;

            InitializeNotifyIcon();

            // Launching the app again shows the running instance (e.g. hidden in the tray)
            Program.ListenForActivation(() => Dispatcher.UIThread.Post(ShowMainWindow));
        }

        // Initialize the theme; must be done after the main window is created.
        // The Material theme also applies its own (XAML-defined) palette in a deferred manner
        // during startup, so apply the custom palette again once that has happened.
        InitializeTheme();
        Dispatcher.UIThread.Post(InitializeTheme, DispatcherPriority.Background);

        base.OnFrameworkInitializationCompleted();
    }

    // The Material theme rebuilds its palette from the base settings whenever the theme variant
    // changes (which also happens during startup), so the custom palette has to be re-applied.
    // Defer it, so that it runs after the Material theme has processed the change itself.
    private void Application_OnActualThemeVariantChanged(object? sender, EventArgs args) =>
        Dispatcher.UIThread.Post(InitializeTheme);

    // Read from the app's own resources, rather than from the executable: when launched through
    // the dotnet host (e.g. 'dotnet run' or debugging), the executable is dotnet.exe instead
    private static byte[]? TryReadAppIcon()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Afterglow/favicon.ico"));
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);

            return buffer.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read the application icon: {ex}");
            return null;
        }
    }

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new NotifyIcon(_mainViewModel.Dashboard.StatusText, TryReadAppIcon());

        // Left click toggles the main window
        _notifyIcon.LeftClicked += (_, _) => Dispatcher.UIThread.Post(ToggleMainWindow);

        // Right click opens the tray menu next to the icon
        _notifyIcon.RightClicked += (_, anchor) =>
            Dispatcher.UIThread.Post(() => ShowTrayMenu(anchor));

        // Clicking the update's notification shows it (what's changed, and a button to update)
        _notifyIcon.NotificationClicked += (_, _) =>
            Dispatcher.UIThread.Post(async () =>
            {
                await ShowMainWindowAsync();
                await _mainViewModel.ShowUpdateAsync(true);
            });

        // A newer version is shown as soon as it's found (see AnnounceUpdateAsync)
        var updateService = _services.GetRequiredService<UpdateService>();

        _notifyIconSubscription = PowerKit.Disposable.Merge(
            _mainViewModel.Dashboard.WatchProperty(
                o => o.StatusText,
                v =>
                {
                    if (_notifyIcon is not null)
                        _notifyIcon.ToolTip = v;
                }
            ),
            updateService.WatchProperty(o => o.AvailableVersion, v => _ = AnnounceUpdateAsync(v))
        );
    }

    // Shows an update that's just been found (e.g. shortly after the PC starts), with what's
    // changed and a button to update, unless it was put off ("Later") for that version. If
    // something is fullscreen (e.g. a game), it isn't interrupted: a notification says so instead
    // (once for each version, as the dashboard keeps showing that an update is available), and
    // the update is shown the next time the window is opened.
    private async Task AnnounceUpdateAsync(Version? version)
    {
        if (version?.ToString(3) is not { } versionText)
            return;

        if (
            string.Equals(
                _settingsService.DismissedUpdateVersion,
                versionText,
                StringComparison.Ordinal
            )
        )
            return;

        var externalApplications = _services.GetRequiredService<ExternalApplicationService>();
        if (
            externalApplications.TryGetForegroundFullScreenMonitor(out var fullScreenMonitor)
            && fullScreenMonitor is not null
        )
        {
            if (
                string.Equals(
                    _settingsService.NotifiedUpdateVersion,
                    versionText,
                    StringComparison.Ordinal
                )
            )
                return;

            _settingsService.NotifiedUpdateVersion = versionText;
            _settingsService.TrySaveToFile();

            _notifyIcon?.ShowNotification(
                string.Format(
                    _mainViewModel.LocalizationManager.UpdateAvailableTitle,
                    Program.Name,
                    versionText
                ),
                _mainViewModel.LocalizationManager.UpdateNotificationText
            );
            return;
        }

        await ShowMainWindowAsync();
        await _mainViewModel.ShowUpdateAsync();
    }

    private void ShowTrayMenu(PlatformInterop.Rect anchor)
    {
        _trayMenu?.Close();

        var trayMenu = new TrayMenuWindow { DataContext = _mainViewModel };
        trayMenu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_trayMenu, trayMenu))
            {
                _trayMenu = null;
                _mainViewModel.Dashboard.IsTrayMenuOpen = false;
            }
        };

        _trayMenu = trayMenu;
        _mainViewModel.Dashboard.IsTrayMenuOpen = true;
        trayMenu.ShowAt(anchor);
    }

    /// <summary>
    /// Shows the main window, creating it the first time. When the app starts hidden (as it
    /// does with Windows), the window isn't created until it's first shown, which may well be
    /// never: that saves about half of the app's memory and threads (for the window's rendering),
    /// for as long as it stays that way.
    /// </summary>
    public static void ShowMainWindow() => _ = ShowMainWindowAsync();

    /// <summary>
    /// Shows the main window (see <see cref="ShowMainWindow" />), and completes once it's ready
    /// to show a dialog (a new window takes a moment to load).
    /// </summary>
    public static Task ShowMainWindowAsync() =>
        (Current as App)?.ShowMainWindowCoreAsync() ?? Task.CompletedTask;

    /// <summary>
    /// Hides the main window to the tray. Once created, it's kept (releasing it again wouldn't
    /// free anything, as the rendering stays loaded, and showing it again would take longer).
    /// </summary>
    public static void HideMainWindow() => Current?.ApplicationLifetime?.TryGetMainWindow()?.Hide();

    public static void ToggleMainWindow()
    {
        if (Current?.ApplicationLifetime?.TryGetMainWindow() is { IsVisible: true })
            HideMainWindow();
        else
            ShowMainWindow();
    }

    private Task ShowMainWindowCoreAsync()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return Task.CompletedTask;

        var loaded = Task.CompletedTask;

        if (desktop.MainWindow is not { } window)
        {
            window = _services.GetRequiredService<ViewManager>().TryBindWindow(_mainViewModel);
            if (window is null)
                return Task.CompletedTask;

            desktop.MainWindow = window;

            var loading = new TaskCompletionSource();
            window.Loaded += (_, _) => loading.TrySetResult();
            loaded = loading.Task;

            // The Material theme applies its own palette to a new window (deferred), so the
            // custom one is applied right away, and again once that has happened
            InitializeTheme();
            Dispatcher.UIThread.Post(InitializeTheme, DispatcherPriority.Background);
        }

        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        window.ShowActivateFocus();

        return loaded;
    }

    private static void TryRun(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Cleanup step failed: {ex}");
        }
    }

    // Last-chance cleanup for when the app is crashing or being terminated, in which case the
    // regular shutdown may be skipped. May run on any thread, so it only does what's safe there.
    // (Gamma and grayscale are restored by their own services in these cases.)
    private void OnProcessEnding()
    {
        TryRun(_mainViewModel.Dashboard.TryFlushPendingSaveToFile);

        // Otherwise the tray icon lingers until the mouse moves over it
        TryRun(() => _notifyIcon?.Dispose());
    }

    public void Dispose()
    {
        // May be called from several places at once (e.g. the exit handler and a crash)
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        Dispatcher.UIThread.UnhandledException -= OnDispatcherUnhandledException;

        // Each step on its own, so that one failing doesn't skip the others
        // (in particular, disposing the services is what restores gamma)
        TryRun(() => _trayMenu?.Close());
        TryRun(() => _notifyIconSubscription?.Dispose());
        TryRun(() => _hueSubscription?.Dispose());
        TryRun(() => _notifyIcon?.Dispose());
        TryRun(_services.Dispose);
    }

    private void OnProcessExit(object? sender, EventArgs args) => OnProcessEnding();

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs args) =>
        OnProcessEnding();

    // On the UI thread, so pending changes can be saved the regular way before the app goes down
    private void OnDispatcherUnhandledException(
        object? sender,
        DispatcherUnhandledExceptionEventArgs args
    ) => TryRun(_mainViewModel.Dashboard.FlushPendingSave);
}

public partial class App
{
    public static void Shutdown(int exitCode = 0)
    {
        if (Current?.ApplicationLifetime?.TryShutdown(exitCode) != true)
            Environment.Exit(exitCode);
    }
}
