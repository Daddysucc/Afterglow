using System;
using System.Runtime.InteropServices;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

public static class KnownFolders
{
    private static readonly Guid DownloadsId = new("374DE290-123F-4565-9164-39C4925E467B");

    /// <summary>
    /// Gets the path of the user's Downloads folder, wherever it has been moved to
    /// (e.g. another drive), or <c>null</c> if it can't be determined.
    /// </summary>
    public static string? TryGetDownloadsDirPath()
    {
        try
        {
            var id = DownloadsId;
            var result = NativeMethods.SHGetKnownFolderPath(ref id, 0, 0, out var pathPointer);

            // The returned memory must be freed whether the call succeeded or not
            try
            {
                return result == 0 ? Marshal.PtrToStringUni(pathPointer) : null;
            }
            finally
            {
                Marshal.FreeCoTaskMem(pathPointer);
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }
}
