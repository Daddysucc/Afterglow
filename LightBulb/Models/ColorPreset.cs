using System.Collections.Generic;
using LightBulb.Core;

namespace LightBulb.Models;

public enum ColorPreset
{
    Recommended,
    LateNight,
    Daylight,

    // Any configuration that doesn't match one of the presets above
    Custom,
}

// Item shown in the preset selector
public record ColorPresetOption(ColorPreset Preset, string DisplayName);

public readonly record struct ColorPresetDefinition(
    ColorPreset Preset,
    ColorConfiguration Day,
    ColorConfiguration Evening,
    ColorConfiguration Night
)
{
    // These are also the default settings
    public static ColorPresetDefinition Recommended { get; } =
        new(ColorPreset.Recommended, new(6600, 1), new(4500, 0.95), new(3900, 0.85));

    public static ColorPresetDefinition LateNight { get; } =
        new(ColorPreset.LateNight, new(6600, 1), new(3600, 0.9), new(2700, 0.8));

    public static ColorPresetDefinition Daylight { get; } =
        new(ColorPreset.Daylight, new(6500, 1), new(6500, 1), new(6500, 1));

    public static IReadOnlyList<ColorPresetDefinition> All { get; } =
    [Recommended, LateNight, Daylight];
}
