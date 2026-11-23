using Afterglow.ViewModels;
using Afterglow.ViewModels.Components;
using Afterglow.ViewModels.Components.Settings;
using Afterglow.ViewModels.Dialogs;
using Afterglow.Views;
using Afterglow.Views.Components;
using Afterglow.Views.Components.Settings;
using Afterglow.Views.Dialogs;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Afterglow.Framework;

public partial class ViewManager
{
    private Control? TryCreateView(ViewModelBase viewModel) =>
        viewModel switch
        {
            MainViewModel => new MainView(),
            DashboardViewModel => new DashboardView(),
            MessageBoxViewModel => new MessageBoxView(),
            SettingsViewModel => new SettingsView(),
            RulesSettingsTabViewModel => new RulesSettingsTabView(),
            CycleSettingsTabViewModel => new CycleSettingsTabView(),
            ControlsSettingsTabViewModel => new ControlsSettingsTabView(),
            SmartLightsSettingsTabViewModel => new SmartLightsSettingsTabView(),
            ScheduleSettingsTabViewModel => new ScheduleSettingsTabView(),
            _ => null,
        };

    public Control? TryBindView(ViewModelBase viewModel)
    {
        var view = TryCreateView(viewModel);
        if (view is null)
            return null;

        view.DataContext ??= viewModel;
        view.Loaded += async (_, _) => await viewModel.InitializeAsync();

        return view;
    }

    public UserControl<T>? TryBindUserControl<T>(T viewModel)
        where T : ViewModelBase => TryBindView(viewModel) as UserControl<T>;

    public Window<T>? TryBindWindow<T>(T viewModel)
        where T : ViewModelBase => TryBindView(viewModel) as Window<T>;
}

public partial class ViewManager : IDataTemplate
{
    bool IDataTemplate.Match(object? data) => data is ViewModelBase;

    Control? ITemplate<object?, Control?>.Build(object? data) =>
        data is ViewModelBase viewModel ? TryBindView(viewModel) : null;
}
