using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Afterglow.Views.Controls;

/// <summary>
/// Shows the first of its children that fits in the width it's given (e.g. a labelled version of
/// something, then a more compact one), or the last one if none of them fit.
/// </summary>
public class FirstFitPanel : Panel
{
    private Control? _shown;

    protected override Size MeasureOverride(Size availableSize)
    {
        _shown = null;

        foreach (var child in Children)
        {
            // How wide it would like to be, with as much room as it wants
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));

            if (_shown is null && child.DesiredSize.Width <= availableSize.Width)
                _shown = child;
        }

        _shown ??= Children.LastOrDefault();

        if (_shown is null)
            return default;

        _shown.Measure(availableSize);
        return _shown.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            // Only the one that's shown takes up any room (the others are arranged out of sight)
            // (and they're out of reach of the mouse and the keyboard too, as they're still there, to
            // be measured)
            var isShown = child == _shown;
            child.Arrange(isShown ? new Rect(finalSize) : default);
            child.Opacity = isShown ? 1 : 0;
            child.IsHitTestVisible = isShown;
            KeyboardNavigation.SetTabNavigation(
                child,
                isShown ? KeyboardNavigationMode.Continue : KeyboardNavigationMode.None
            );
        }

        return finalSize;
    }
}
