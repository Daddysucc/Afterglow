using System;
using System.Globalization;
using Afterglow.Utils;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Afterglow.Converters;

public class TimeOnlyToStringConverter : IValueConverter
{
    public static TimeOnlyToStringConverter Instance { get; } = new();

    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is TimeOnly timeOfDay ? timeOfDay.ToString(null, culture) : default;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        // (also a plain hour, e.g. "22"; anything that isn't a time leaves the value as it was)
        NumberInput.TryParseTimeOfDay(value as string, culture, out var result)
            ? result
            : BindingOperations.DoNothing;
}
