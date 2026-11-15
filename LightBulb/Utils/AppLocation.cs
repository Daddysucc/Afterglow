using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LightBulb.PlatformInterop;

namespace LightBulb.Utils;

internal static class AppLocation
{
    /// <summary>
    /// Whether the given folder is somewhere files don't stay for long: the Downloads folder
    /// (which people tidy up), or a temporary folder (e.g. when the app is run straight from
    /// inside a zip file, which Windows extracts to one).
    /// </summary>
    public static bool IsTemporary(string dirPath) =>
        IsTemporary(
            dirPath,
            [
                KnownFolders.TryGetDownloadsDirPath(),
                Path.GetTempPath(),
                // The temporary folder may be reported in its short (8.3) form, so also check
                // its usual full location
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Temp"
                ),
            ]
        );

    internal static bool IsTemporary(string dirPath, IEnumerable<string?> temporaryDirPaths) =>
        temporaryDirPaths.Any(t => !string.IsNullOrWhiteSpace(t) && IsWithin(dirPath, t));

    private static bool IsWithin(string path, string parentPath)
    {
        static string Normalize(string path) =>
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)) + Path.DirectorySeparatorChar;

        try
        {
            return Normalize(path)
                .StartsWith(Normalize(parentPath), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
            when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
