using Afterglow.Framework;
using Afterglow.ViewModels.Components.Settings;
using Avalonia.Interactivity;

namespace Afterglow.Views.Components.Settings;

public partial class CycleSettingsTabView : UserControl<CycleSettingsTabViewModel>
{
    public CycleSettingsTabView() => InitializeComponent();

    // Shows a numbered badge on each display, matching the numbers in the list
    private void IdentifyButton_OnClick(object? sender, RoutedEventArgs args)
    {
        foreach (var monitor in DataContext.Monitors)
        {
            new MonitorBadgeWindow(
                monitor.Number,
                monitor.DisplayName,
                monitor.IsPrimary
                    ? DataContext.LocalizationManager.PrimaryBadge
                    : monitor.Monitor.GdiDeviceName
            ).ShowOn(monitor.Monitor.Bounds);
        }
    }
}
