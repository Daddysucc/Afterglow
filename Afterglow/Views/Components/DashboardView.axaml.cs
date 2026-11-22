using Afterglow.Framework;
using Afterglow.ViewModels.Components;
using Avalonia.Input;
using PowerKit.Extensions;

namespace Afterglow.Views.Components;

public partial class DashboardView : UserControl<DashboardViewModel>
{
    public DashboardView() => InitializeComponent();

    private void ConfigurationOffsetStackPanel_OnPointerReleased(
        object? sender,
        PointerReleasedEventArgs args
    ) => DataContext.ResetConfigurationOffsetCommand.ExecuteIfCan(null);
}
