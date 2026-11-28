using Afterglow.Framework;
using Afterglow.ViewModels.Dialogs;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Afterglow.Views.Dialogs;

public partial class SettingsView : UserControl<SettingsViewModel>
{
    public SettingsView() => InitializeComponent();

    // The settings page covers the main window's title bar, so it needs to support dragging too
    private void HeaderBorder_OnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
            window.BeginMoveDrag(args);
    }

    // Hides the window to the tray, as the dashboard's button does (the settings stay open)
    private void HideButton_OnClick(object? sender, RoutedEventArgs args) => App.HideMainWindow();
}
