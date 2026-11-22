using Afterglow.Framework;
using Afterglow.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Afterglow.Views;

public partial class MainView : Window<MainViewModel>
{
    public MainView() => InitializeComponent();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Let the dashboard skip refreshing its visuals while the window can't be seen
        if (
            (change.Property == IsVisibleProperty || change.Property == WindowStateProperty)
            && base.DataContext is MainViewModel mainViewModel
        )
        {
            mainViewModel.Dashboard.IsDashboardVisible =
                IsVisible && WindowState != WindowState.Minimized;
        }
    }

    private void Window_OnLoaded(object? sender, RoutedEventArgs args)
    {
        // If the app is set to start hidden (by launch argument or setting), hide the window,
        // unless a dialog is open
        if (DataContext.IsStartHidden && !DialogHost.IsOpen)
            Hide();
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs args)
    {
        // When the user tries to close the window, hide it instead
        if (args.CloseReason == WindowCloseReason.WindowClosing)
        {
            args.Cancel = true;
            Hide();
        }
    }

    private void HeaderBorder_OnPointerPressed(object? sender, PointerPressedEventArgs args) =>
        BeginMoveDrag(args);

    private void HideButton_OnClick(object sender, RoutedEventArgs args) => Hide();
}
