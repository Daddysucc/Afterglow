using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Afterglow.Utils;

/// <summary>
/// Reads numbers typed into settings, as they're shown (with or without their units, e.g. "×2.5",
/// "3,200 K", "45%", "1.5 s"). The settings themselves keep the values within their bounds.
/// </summary>
internal static class NumberInput
{
    /// <summary>
    /// Reads the number in the given text, ignoring anything around it (units, signs of
    /// multiplication, spaces), or returns false if there isn't one.
    /// </summary>
    public static bool TryParse(string? text, CultureInfo culture, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var number = new StringBuilder();
        foreach (var c in text)
        {
            if (char.IsDigit(c))
                number.Append(c);
            // (the minus sign that values are shown with, as well as the one on the keyboard)
            else if (c is '-' or '−')
                number.Append('-');
            else if (c == '+')
                number.Append('+');
            else if (
                culture.NumberFormat.NumberDecimalSeparator.Contains(c)
                || culture.NumberFormat.NumberGroupSeparator.Contains(c)
                // (a group separator that's a space, as in French, is often typed as a plain one)
                || (c == ' ' && culture.NumberFormat.NumberGroupSeparator.Any(char.IsWhiteSpace))
            )
                number.Append(c == ' ' ? culture.NumberFormat.NumberGroupSeparator : c.ToString());
        }

        return double.TryParse(
            number.ToString().Trim(),
            NumberStyles.Float | NumberStyles.AllowThousands,
            culture,
            out value
        );
    }

    /// <summary>
    /// Reads a duration typed as it's shown ("01:30:00", "1:30"), or as a plain number of
    /// minutes ("90"), or returns false if it's neither.
    /// </summary>
    public static bool TryParseDuration(string? text, CultureInfo culture, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.Contains(':'))
            return TimeSpan.TryParse(text.Trim(), culture, out duration);

        if (!TryParse(text, culture, out var minutes))
            return false;

        duration = TimeSpan.FromMinutes(Math.Clamp(minutes, -1e6, 1e6));
        return true;
    }

    /// <summary>
    /// Reads a time of day typed as it's shown ("22:30", "10:30 PM"), or as a plain hour ("22",
    /// "6.5" for half past six), or returns false if it's neither.
    /// </summary>
    public static bool TryParseTimeOfDay(string? text, CultureInfo culture, out TimeOnly time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (TimeOnly.TryParse(text.Trim(), culture, DateTimeStyles.None, out time))
            return true;

        if (!text.Contains(':') && TryParse(text, culture, out var hours) && hours is >= 0 and < 24)
        {
            time = TimeOnly.FromTimeSpan(TimeSpan.FromHours(hours));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads a duration in minutes, typed as minutes ("45", "45 min"), hours ("2 h"), or both
    /// ("1 h 15 min", "1h15", "1:15"), with the hours' unit as the given format shows it (e.g.
    /// "{0} h"), or returns false if there's no number in it.
    /// </summary>
    public static bool TryParseMinutes(
        string? text,
        CultureInfo culture,
        string hoursFormat,
        out double minutes
    )
    {
        minutes = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // "1:15"
        var colon = text.IndexOf(':');
        if (colon > 0)
        {
            if (
                !TryParse(text[..colon], culture, out var hours)
                || !TryParse(text[(colon + 1)..], culture, out var rest)
            )
                return false;

            minutes = hours * 60 + rest;
            return true;
        }

        var hoursUnit = hoursFormat.Replace("{0}", "").Trim();
        var numbers = Regex.Matches(text, @"(\d+(?:[.,]\d+)?)\s*([^\d\s:]*)");
        if (numbers.Count <= 0)
            return false;

        var isAnyFound = false;
        for (var i = 0; i < numbers.Count; i++)
        {
            if (!TryParse(numbers[i].Groups[1].Value, culture, out var number))
                continue;

            isAnyFound = true;
            var unit = numbers[i].Groups[2].Value;

            // A number in hours (by its unit, or as the first of two, as in "1h15")
            var isHours =
                (
                    hoursUnit.Length > 0
                    && unit.StartsWith(hoursUnit, StringComparison.CurrentCultureIgnoreCase)
                ) || (numbers.Count == 2 && i == 0 && unit.Length == 0);

            minutes += isHours ? number * 60 : number;
        }

        return isAnyFound;
    }
}
