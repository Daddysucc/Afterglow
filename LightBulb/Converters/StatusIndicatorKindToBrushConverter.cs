using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LightBulb.Models;

namespace LightBulb.Converters;

public class StatusIndicatorKindToBrushConverter : IValueConverter
{
    public static StatusIndicatorKindToBrushConverter Instance { get; } = new();

    // Same colors as the stage bar on the dashboard graph
    private static readonly IBrush DayBrush = new SolidColorBrush(Color.Parse("#5B9BD5"));
    private static readonly IBrush EveningBrush = new SolidColorBrush(Color.Parse("#F4B942"));
    private static readonly IBrush NightBrush = new SolidColorBrush(Color.Parse("#E85D3F"));
    private static readonly IBrush PausedBrush = new SolidColorBrush(Color.Parse("#7D8794"));

    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value switch
        {
            StatusIndicatorKind.Day => DayBrush,
            StatusIndicatorKind.Night => NightBrush,
            StatusIndicatorKind.Paused => PausedBrush,
            // Evening and transitions share the same warm gold
            _ => EveningBrush,
        };

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
