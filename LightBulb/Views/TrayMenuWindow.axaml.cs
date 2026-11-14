using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LightBulb.Framework;
using LightBulb.ViewModels;

namespace LightBulb.Views;

/// <summary>
/// Custom-styled replacement for the native tray icon context menu.
/// </summary>
public partial class TrayMenuWindow : Window<MainViewModel>
{
    private PixelPoint _anchor;
    private bool _isClosing;

    public TrayMenuWindow() => InitializeComponent();

    /// <summary>
    /// Shows the menu next to the given point (in physical screen coordinates),
    /// typically the cursor position when the tray icon was clicked.
    /// </summary>
    public void ShowAt(PixelPoint anchor)
    {
        _anchor = anchor;

        Position = anchor;
        Show();

        // Size is known once the window has been laid out
        UpdatePosition();
        Activate();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UpdatePosition();
    }

    // Opens upwards and to the right of the anchor (like native tray menus with a bottom taskbar),
    // flipping and clamping as needed so that the menu always stays within the screen's work area
    private void UpdatePosition()
    {
        var screen = Screens.ScreenFromPoint(_anchor) ?? Screens.Primary;
        if (screen is null)
            return;

        var area = screen.WorkingArea;
        var width = (int)Math.Ceiling(Bounds.Width * screen.Scaling);
        var height = (int)Math.Ceiling(Bounds.Height * screen.Scaling);

        var x = _anchor.X;
        var y = _anchor.Y - height;

        if (x + width > area.Right)
            x = _anchor.X - width;

        if (y < area.Y)
            y = _anchor.Y;

        x = Math.Clamp(x, area.X, Math.Max(area.X, area.Right - width));
        y = Math.Clamp(y, area.Y, Math.Max(area.Y, area.Bottom - height));

        Position = new PixelPoint(x, y);
    }

    private void CloseMenu()
    {
        if (_isClosing)
            return;

        _isClosing = true;
        Close();
    }

    // Clicking anywhere outside of the menu deactivates it
    private void Window_OnDeactivated(object? sender, EventArgs args) => CloseMenu();

    private void Window_OnKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key != Key.Escape)
            return;

        CloseMenu();
        args.Handled = true;
    }

    // Close after the command has run (the command is executed after the click event)
    private void MenuItem_OnClick(object? sender, RoutedEventArgs args) =>
        Dispatcher.UIThread.Post(CloseMenu);
}
