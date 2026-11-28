using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Afterglow.Views.Controls;

/// <summary>
/// A text box for a setting's value, next to its slider, to type it in exactly. What's typed is
/// applied when the box loses focus or Enter is pressed, and the box then always shows the value
/// as it was stored: kept within its bounds and rounded as the setting does it, or left as it was
/// if what was typed isn't a value at all. Escape puts back what's stored.
/// </summary>
public class ValueTextBox : TextBox
{
    // Looks and behaves like any other text box
    protected override Type StyleKeyOverride => typeof(TextBox);

    private BindingExpressionBase? TextBinding =>
        BindingOperations.GetBindingExpressionBase(this, TextProperty);

    // Shows the value as it's stored (after the setting has taken in what was typed)
    private void ShowStoredValue() => TextBinding?.UpdateTarget();

    protected override void OnKeyDown(KeyEventArgs args)
    {
        if (args.Key == Key.Enter)
        {
            TextBinding?.UpdateSource();
            ShowStoredValue();
            SelectAll();
            args.Handled = true;
            return;
        }

        if (args.Key == Key.Escape)
        {
            ShowStoredValue();
            SelectAll();
            args.Handled = true;
            return;
        }

        base.OnKeyDown(args);
    }

    protected override void OnLostFocus(FocusChangedEventArgs args)
    {
        base.OnLostFocus(args);

        // (after the binding has applied what was typed, which it does as focus is lost)
        Dispatcher.UIThread.Post(ShowStoredValue);
    }
}
