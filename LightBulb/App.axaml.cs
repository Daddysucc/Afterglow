using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using LightBulb.Framework;
using LightBulb.Localization;
using LightBulb.PlatformInterop;
using LightBulb.Services;
using LightBulb.Utils.Extensions;
using LightBulb.ViewModels;
using LightBulb.ViewModels.Components;
using LightBulb.ViewModels.Components.Settings;
using LightBulb.ViewModels.Dialogs;
using LightBulb.Views;
using Material.Styles.Themes;
using Microsoft.Extensions.DependencyInjection;
using PowerKit.Extensions;

namespace LightBulb;

public partial class App : Application, IDisposable
{
    private readonly ServiceProvider _services;
    private readonly SettingsService _settingsService;
    private readonly MainViewModel _mainViewModel;

    private NotifyIcon? _notifyIcon;
    private IDisposable? _notifyIconSubscription;
    private TrayMenuWindow? _trayMenu;
    private bool _isDisposed;

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
        services.AddSingleton<HotKeyService>();
        services.AddSingleton<SettingsService>();

        // View models
        services.AddTransient<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddTransient<MessageBoxViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsTabViewModelBase, CycleSettingsTabViewModel>();
        services.AddTransient<SettingsTabViewModelBase, ScheduleSettingsTabViewModel>();
        services.AddTransient<SettingsTabViewModelBase, RulesSettingsTabViewModel>();
        services.AddTransient<SettingsTabViewModelBase, ControlsSettingsTabViewModel>();

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

            InitializeNotifyIcon();
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
            using var stream = AssetLoader.Open(new Uri("avares://LightBulb/favicon.ico"));
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
                _trayMenu = null;
        };

        _trayMenu = trayMenu;
        trayMenu.ShowAt(anchor);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _trayMenu?.Close();
        _notifyIconSubscription?.Dispose();
        _notifyIcon?.Dispose();
        _services.Dispose();
    }
}

public partial class App
{
    public static void Shutdown(int exitCode = 0)
    {
        if (Current?.ApplicationLifetime?.TryShutdown(exitCode) != true)
            Environment.Exit(exitCode);
    }
}
