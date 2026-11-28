using System;
using System.Globalization;
using Afterglow.Utils;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Afterglow.Converters;

public class FractionToPercentageStringConverter : IValueConverter
{
    public static FractionToPercentageStringConverter Instance { get; } = new();

    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is double doubleValue ? doubleValue.ToString("P0", culture) : default;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        // (anything that isn't a number leaves the value as it was)
        NumberInput.TryParse(value as string, culture, out var result)
            ? result / 100.0
            : BindingOperations.DoNothing;
}
