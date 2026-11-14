using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using LightBulb.Core;
using LightBulb.Framework;
using LightBulb.PlatformInterop;
using LightBulb.ViewModels;

namespace LightBulb.Views;

/// <summary>
/// Custom-styled replacement for the native tray icon context menu.
/// </summary>
public partial class TrayMenuWindow : Window<MainViewModel>
{
    // Space between the tray icon and the menu, in logical pixels
    private const double Gap = 8;

    private ScreenRect _anchor;
    private bool _isClosing;

    public TrayMenuWindow() => InitializeComponent();

    /// <summary>
    /// Shows the menu next to the given area (typically the tray icon, or the cursor position),
    /// in physical screen pixels.
    /// </summary>
    public void ShowAt(PlatformInterop.Rect anchor)
    {
        _anchor = new ScreenRect(anchor.Left, anchor.Top, anchor.Right, anchor.Bottom);

        // Position the window before it's shown, so that it doesn't flash elsewhere first...
        UpdatePosition(GetContentSize());

        Show();
        Activate();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // ...then again once it's open. Note that the window's own bounds are not reliable at
        // this point: it's only sized to its content afterwards, so use the content size instead.
        UpdatePosition(GetContentSize());
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Re-anchor whenever the size settles or changes (e.g. after moving to a display
        // with a different scaling), so that the menu stays next to the icon and on screen
        if (change.Property == ClientSizeProperty && IsVisible)
            UpdatePosition(GetContentSize());
    }

    // Size of the window's content, in logical pixels
    private Size GetContentSize()
    {
        if (Content is not Layoutable content)
            return ClientSize;

        content.Measure(Size.Infinity);
        return content.DesiredSize;
    }

    private void UpdatePosition(Size size)
    {
        // Everything here is in physical pixels, as reported by the system for the target display
        var (workArea, scale) = DisplayMonitors.GetWorkAreaAt(_anchor.CenterX, _anchor.CenterY);
        if (workArea == PlatformInterop.Rect.Empty)
            return;

        var (x, y) = PopupPlacement.Calculate(
            _anchor,
            (int)Math.Ceiling(size.Width * scale),
            (int)Math.Ceiling(size.Height * scale),
            new ScreenRect(workArea.Left, workArea.Top, workArea.Right, workArea.Bottom),
            (int)Math.Round(Gap * scale)
        );

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
