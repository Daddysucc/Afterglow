using System;

namespace LightBulb.Core;

/// <summary>
/// Rectangle in physical screen pixels.
/// </summary>
public readonly record struct ScreenRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;

    public int CenterX => Left + Width / 2;

    public int CenterY => Top + Height / 2;
}

public static class PopupPlacement
{
    /// <summary>
    /// Positions a popup of the given size next to an anchor (e.g. a tray icon), on the side that
    /// faces away from the taskbar, keeping it entirely within the work area.
    /// All values are in physical screen pixels.
    /// </summary>
    /// <param name="anchor">Area to place the popup next to (may be empty, e.g. a cursor position).</param>
    /// <param name="width">Width of the popup.</param>
    /// <param name="height">Height of the popup.</param>
    /// <param name="workArea">Work area of the display (excluding the taskbar).</param>
    /// <param name="gap">Space to leave between the anchor and the popup.</param>
    public static (int X, int Y) Calculate(
        ScreenRect anchor,
        int width,
        int height,
        ScreenRect workArea,
        int gap = 0
    )
    {
        int x,
            y;

        // Taskbar at the bottom: open upwards
        if (anchor.Top >= workArea.Bottom)
        {
            x = anchor.CenterX - width / 2;
            y = anchor.Top - gap - height;
        }
        // Taskbar at the top: open downwards
        else if (anchor.Bottom <= workArea.Top)
        {
            x = anchor.CenterX - width / 2;
            y = anchor.Bottom + gap;
        }
        // Taskbar on the left: open to the right
        else if (anchor.Right <= workArea.Left)
        {
            x = anchor.Right + gap;
            y = anchor.CenterY - height / 2;
        }
        // Taskbar on the right: open to the left
        else if (anchor.Left >= workArea.Right)
        {
            x = anchor.Left - gap - width;
            y = anchor.CenterY - height / 2;
        }
        // Anchor within the work area (e.g. the overflow flyout, or the cursor position):
        // open upwards if there's room, like native menus do, otherwise downwards
        else
        {
            x = anchor.CenterX - width / 2;
            y = anchor.Top - gap - height;

            if (y < workArea.Top)
                y = anchor.Bottom + gap;
        }

        // Keep the popup within the work area (if it's larger, align it with the top-left corner)
        x = Math.Max(workArea.Left, Math.Min(x, workArea.Right - width));
        y = Math.Max(workArea.Top, Math.Min(y, workArea.Bottom - height));

        return (x, y);
    }
}
