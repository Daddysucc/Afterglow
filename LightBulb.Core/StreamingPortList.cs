using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LightBulb.Core;

/// <summary>
/// List of TCP ports, as entered by the user (e.g. "48010, 27036").
/// </summary>
public static class StreamingPortList
{
    public const string Default = "48010";

    private static readonly char[] Separators = [',', ';', ' ', '\t'];

    /// <summary>
    /// Parses a list of ports separated by commas (or semicolons/spaces).
    /// Entries that are not valid TCP ports (1-65535) are ignored, as are duplicates.
    /// </summary>
    public static IReadOnlyList<int> Parse(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                    int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var port)
                        ? port
                        : 0
                )
                .Where(p => p is >= 1 and <= 65535)
                .Distinct()
                .ToArray();

    /// <summary>
    /// Formats ports in the canonical form, e.g. "48010, 27036".
    /// </summary>
    public static string Format(IEnumerable<int> ports) =>
        string.Join(", ", ports.Select(p => p.ToString(CultureInfo.InvariantCulture)));

    /// <summary>
    /// Re-formats user input in the canonical form, dropping anything that isn't a valid port.
    /// </summary>
    public static string Normalize(string? value) => Format(Parse(value));
}
