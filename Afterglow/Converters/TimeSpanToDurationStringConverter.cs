using System;
using System.Globalization;
using Afterglow.Utils;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Afterglow.Converters;

public class TimeSpanToDurationStringConverter : IValueConverter
{
    public static TimeSpanToDurationStringConverter Instance { get; } = new();

    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is TimeSpan timeSpanValue ? timeSpanValue.ToString(@"hh\:mm\:ss", culture) : default;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        // (also a plain number of minutes, e.g. "90"; anything that isn't a duration leaves the
        // value as it was)
        NumberInput.TryParseDuration(value as string, culture, out var result)
            ? result
            : BindingOperations.DoNothing;
}
