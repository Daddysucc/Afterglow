using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Afterglow.ViewModels.Components.Settings;

/// <summary>
/// A room (or zone) on the Hue bridge, which can be chosen to follow the cycle.
/// </summary>
public class HueRoomViewModel(
    string id,
    string name,
    bool isSelected,
    Action<string, bool> setSelected
) : ObservableObject
{
    private bool _isSelected = isSelected;

    public string Id { get; } = id;

    public string Name { get; } = name;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
                setSelected(Id, value);
        }
    }

    // Reflects the saved choice, without saving it again
    internal void Refresh(bool isSelected) =>
        SetProperty(ref _isSelected, isSelected, nameof(IsSelected));
}
