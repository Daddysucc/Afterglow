using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
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

        // Initialize the lifetime
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewManager = _services.GetRequiredService<ViewManager>();

            desktop.MainWindow = viewManager.TryBindWindow(_mainViewModel);

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
        _notifyIcon.LeftClicked += (_, _) =>
            Dispatcher.UIThread.Post(() => ApplicationLifetime?.TryGetMainWindow()?.Toggle());

        // Right click opens the tray menu next to the icon
        _notifyIcon.RightClicked += (_, anchor) =>
            Dispatcher.UIThread.Post(() => ShowTrayMenu(anchor));

        _notifyIconSubscription = _mainViewModel.Dashboard.WatchProperty(
            o => o.StatusText,
            v =>
            {
                if (_notifyIcon is not null)
                    _notifyIcon.ToolTip = v;
            }
        );
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

    private void ShowMainWindow()
    {
        if (ApplicationLifetime?.TryGetMainWindow() is not { } window)
            return;

        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        window.ShowActivateFocus();
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
