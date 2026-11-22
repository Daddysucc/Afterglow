using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Afterglow.Views;

/// <summary>
/// Badge shown in the corner of a display to identify it. It never takes focus,
/// lets clicks pass through, and closes itself after a short while.
/// </summary>
public partial class MonitorBadgeWindow : Window
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(2.5);

    // Distance from the corner of the display, in physical pixels
    private const int CornerOffset = 32;

    public MonitorBadgeWindow() => InitializeComponent();

    public MonitorBadgeWindow(int number, string name, string details)
        : this()
    {
        NumberTextBlock.Text = number.ToString();
        NameTextBlock.Text = name;
        DetailsTextBlock.Text = details;
    }

    /// <summary>
    /// Shows the badge in the top-left corner of a display with the given bounds
    /// (in physical pixels, as reported by the system, so that DPI scaling doesn't offset it).
    /// </summary>
    public void ShowOn(PlatformInterop.Rect bounds)
    {
        Position = new PixelPoint(bounds.Left + CornerOffset, bounds.Top + CornerOffset);
        Show();

        DispatcherTimer.RunOnce(Close, Lifetime);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Only real Win32 windows can be turned into overlays
        if (
            TryGetPlatformHandle() is { HandleDescriptor: "HWND", Handle: var handle }
            && handle != 0
        )
        {
            PlatformInterop.Window.MakeOverlay(handle);
        }
    }
}
