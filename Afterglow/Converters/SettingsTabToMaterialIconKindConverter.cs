using System;
using System.Globalization;
using Afterglow.ViewModels.Components.Settings;
using Avalonia.Data.Converters;
using Material.Icons;

namespace Afterglow.Converters;

public class SettingsTabToMaterialIconKindConverter : IValueConverter
{
    public static SettingsTabToMaterialIconKindConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            CycleSettingsTabViewModel => MaterialIconKind.Palette,
            ScheduleSettingsTabViewModel => MaterialIconKind.MapMarkerOutline,
            RulesSettingsTabViewModel => MaterialIconKind.PauseCircleOutline,
            ControlsSettingsTabViewModel => MaterialIconKind.Tune,
            SmartLightsSettingsTabViewModel => MaterialIconKind.LightbulbGroupOutline,
            // Shouldn't happen
            _ => MaterialIconKind.QuestionMark,
        };

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
