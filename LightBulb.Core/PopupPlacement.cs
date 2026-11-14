using System;

namespace LightBulb.Core;

/// <summary>
/// Rectangle in physical screen pixels. Coordinates may be negative
/// (e.g. on displays placed to the left of, or above, the primary display).
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
    private enum TaskbarEdge
    {
        None,
        Bottom,
        Top,
        Left,
        Right,
    }

    private static TaskbarEdge DetectTaskbarEdge(
        ScreenRect anchor,
        ScreenRect workArea,
        ScreenRect? displayBounds
    )
    {
        // A regular taskbar is excluded from the work area, so its icons are outside of it
        if (anchor.Top >= workArea.Bottom)
            return TaskbarEdge.Bottom;

        if (anchor.Bottom <= workArea.Top)
            return TaskbarEdge.Top;

        if (anchor.Right <= workArea.Left)
            return TaskbarEdge.Left;

        if (anchor.Left >= workArea.Right)
            return TaskbarEdge.Right;

        // An auto-hidden taskbar is not excluded from the work area, so the work area spans the
        // whole display on that side, and the icon sits right at the display's edge. (Icons in the
        // overflow flyout, or a cursor position, are farther from the edge than that.)
        if (displayBounds is not { } display || anchor.Width <= 0 || anchor.Height <= 0)
            return TaskbarEdge.None;

        // The taskbar is only slightly thicker than its icons
        var threshold = Math.Max(anchor.Width, anchor.Height) * 2;

        var candidates = new (TaskbarEdge Edge, int Distance, bool IsUnreserved)[]
        {
            (TaskbarEdge.Bottom, display.Bottom - anchor.Bottom, workArea.Bottom >= display.Bottom),
            (TaskbarEdge.Top, anchor.Top - display.Top, workArea.Top <= display.Top),
            (TaskbarEdge.Left, anchor.Left - display.Left, workArea.Left <= display.Left),
            (TaskbarEdge.Right, display.Right - anchor.Right, workArea.Right >= display.Right),
        };

        var closest = TaskbarEdge.None;
        var closestDistance = int.MaxValue;

        foreach (var (edge, distance, isUnreserved) in candidates)
        {
            if (isUnreserved && distance <= threshold && distance < closestDistance)
            {
                closest = edge;
                closestDistance = distance;
            }
        }

        return closest;
    }

    /// <summary>
    /// Positions a popup of the given size next to an anchor (e.g. a tray icon), on the side that
    /// faces away from the taskbar, keeping it entirely within the work area.
    /// All values are in physical screen pixels.
    /// </summary>
    /// <param name="anchor">Area to place the popup next to (may be empty, e.g. a cursor position).</param>
    /// <param name="width">Width of the popup.</param>
    /// <param name="height">Height of the popup.</param>
    /// <param name="workArea">Work area of the display (excluding the taskbar, unless it's auto-hidden).</param>
    /// <param name="gap">Space to leave between the anchor and the popup.</param>
    /// <param name="displayBounds">Full bounds of the display, used to detect an auto-hidden taskbar.</param>
    public static (int X, int Y) Calculate(
        ScreenRect anchor,
        int width,
        int height,
        ScreenRect workArea,
        int gap = 0,
        ScreenRect? displayBounds = null
    )
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);

        int x,
            y;

        switch (DetectTaskbarEdge(anchor, workArea, displayBounds))
        {
            // Taskbar at the bottom: open upwards
            case TaskbarEdge.Bottom:
                x = anchor.CenterX - width / 2;
                y = anchor.Top - gap - height;
                break;

            // Taskbar at the top: open downwards
            case TaskbarEdge.Top:
                x = anchor.CenterX - width / 2;
                y = anchor.Bottom + gap;
                break;

            // Taskbar on the left: open to the right
            case TaskbarEdge.Left:
                x = anchor.Right + gap;
                y = anchor.CenterY - height / 2;
                break;

            // Taskbar on the right: open to the left
            case TaskbarEdge.Right:
                x = anchor.Left - gap - width;
                y = anchor.CenterY - height / 2;
                break;

            // Anchor within the work area (e.g. the overflow flyout, or the cursor position):
            // open upwards if there's room, like native menus do, otherwise downwards
            default:
                x = anchor.CenterX - width / 2;
                y = anchor.Top - gap - height;

                if (y < workArea.Top)
                    y = anchor.Bottom + gap;

                break;
        }

        // Keep the popup within the work area (if it's larger, align it with the top-left corner)
        x = Math.Max(workArea.Left, Math.Min(x, workArea.Right - width));
        y = Math.Max(workArea.Top, Math.Min(y, workArea.Bottom - height));

        return (x, y);
    }
}
