using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Afterglow.ViewModels.Components.Settings;

/// <summary>
/// A light in one of the chosen rooms, which can be left out of following the cycle.
/// </summary>
public partial class HueLightViewModel(
    string id,
    string name,
    string? stateText,
    bool isIncluded,
    Action<string, bool> setIncluded,
    Action<string>? followAgain = null,
    string? followAgainText = null,
    string? followAgainTooltip = null
) : ObservableObject
{
    private bool _isIncluded = isIncluded;

    public string Id { get; } = id;

    public string Name { get; } = name;

    // e.g. "Following", or "Not responding"
    public string? StateText { get; } = stateText;

    public bool HasStateText => !string.IsNullOrEmpty(StateText);

    // Left alone, as it seemed to be changed by something else, but can follow again
    public bool CanFollowAgain => followAgain is not null;

    public string? FollowAgainText { get; } = followAgainText;

    public string? FollowAgainTooltip { get; } = followAgainTooltip;

    public bool IsIncluded
    {
        get => _isIncluded;
        set
        {
            if (SetProperty(ref _isIncluded, value))
                setIncluded(Id, value);
        }
    }

    [RelayCommand]
    private void FollowAgain() => followAgain?.Invoke(Id);
}

/// <summary>
/// The lights in one of the chosen rooms (or zones).
/// </summary>
public class HueLightGroupViewModel(string name, IReadOnlyList<HueLightViewModel> lights)
{
    public string Name { get; } = name;

    public IReadOnlyList<HueLightViewModel> Lights { get; } = lights;
}
