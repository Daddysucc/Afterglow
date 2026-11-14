using Avalonia.Controls;
using Avalonia.Input;
using LightBulb.Framework;
using LightBulb.ViewModels.Dialogs;

namespace LightBulb.Views.Dialogs;

public partial class SettingsView : UserControl<SettingsViewModel>
{
    public SettingsView() => InitializeComponent();

    // The settings page covers the main window's title bar, so it needs to support dragging too
    private void HeaderBorder_OnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
            window.BeginMoveDrag(args);
    }
}
