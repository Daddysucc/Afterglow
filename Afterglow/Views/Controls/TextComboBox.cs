using System;
using Avalonia;
using Avalonia.Controls;

namespace Afterglow.Views.Controls;

/// <summary>
/// A drop-down of fixed items whose text can change while one of them is selected (e.g. when the
/// language changes), and that always shows the selected one's current text.
/// </summary>
/// <remarks>
/// A closed <see cref="ComboBox" /> shows what the selected item said when it was selected, and
/// doesn't notice it saying something else later, so it kept showing the old language until
/// another item was picked. This shows the new text right away (and only follows the selected
/// item, so it costs nothing otherwise).
/// </remarks>
public class TextComboBox : ComboBox
{
    private ComboBoxItem? _selectedItem;

    // (looks like any other drop-down)
    protected override Type StyleKeyOverride => typeof(ComboBox);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != SelectedItemProperty)
            return;

        if (_selectedItem is not null)
            _selectedItem.PropertyChanged -= SelectedItem_OnPropertyChanged;

        _selectedItem = SelectedItem as ComboBoxItem;

        if (_selectedItem is not null)
            _selectedItem.PropertyChanged += SelectedItem_OnPropertyChanged;
    }

    private void SelectedItem_OnPropertyChanged(
        object? sender,
        AvaloniaPropertyChangedEventArgs args
    )
    {
        if (
            args.Property == ContentControl.ContentProperty
            && ReferenceEquals(sender, SelectedItem)
        )
            SelectionBoxItem = args.NewValue;
    }
}
