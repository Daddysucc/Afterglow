using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

internal static class IconHandle
{
    private const int SmCxSmIcon = 49;
    private const int SmCySmIcon = 50;
    private const int SmCxIcon = 11;
    private const int SmCyIcon = 12;

    // Version of the icon format expected by CreateIconFromResourceEx
    private const uint IconResourceVersion = 0x00030000;

    public static (int Width, int Height) GetLargeIconSize()
    {
        var width = NativeMethods.GetSystemMetrics(SmCxIcon);
        var height = NativeMethods.GetSystemMetrics(SmCyIcon);

        return (width > 0 ? width : 32, height > 0 ? height : 32);
    }

    public static (int Width, int Height) GetSmallIconSize()
    {
        var width = NativeMethods.GetSystemMetrics(SmCxSmIcon);
        var height = NativeMethods.GetSystemMetrics(SmCySmIcon);

        return (width > 0 ? width : 16, height > 0 ? height : 16);
    }

    /// <summary>
    /// Creates an icon handle from the contents of an .ico file, using the image that best
    /// matches the requested size. Returns 0 on failure. The caller owns the handle.
    /// </summary>
    public static nint TryCreateFromIco(byte[] icoData, int width, int height)
    {
        try
        {
            // ICONDIR: reserved (2), type (2, 1 = icon), image count (2)
            if (icoData.Length < 6 || BitConverter.ToUInt16(icoData, 2) != 1)
                return 0;

            var count = BitConverter.ToUInt16(icoData, 4);

            // ICONDIRENTRY (16 bytes each): width (1, 0 = 256), height (1, 0 = 256), colors (1),
            // reserved (1), planes (2), bits per pixel (2), image size (4), image offset (4).
            // Pick the smallest image that is at least as large as requested (to downscale crisply),
            // or the largest one if all of them are smaller. Prefer higher color depth on ties.
            var bestIndex = -1;
            var bestSize = 0;
            var bestBitCount = 0;

            for (var i = 0; i < count; i++)
            {
                var entryOffset = 6 + i * 16;
                if (entryOffset + 16 > icoData.Length)
                    break;

                var size = icoData[entryOffset] == 0 ? 256 : icoData[entryOffset];
                var bitCount = BitConverter.ToUInt16(icoData, entryOffset + 6);

                var isBetter =
                    bestIndex < 0
                    || (size >= width && (bestSize < width || size < bestSize))
                    || (size < width && bestSize < width && size > bestSize)
                    || (size == bestSize && bitCount > bestBitCount);

                if (isBetter)
                {
                    bestIndex = i;
                    bestSize = size;
                    bestBitCount = bitCount;
                }
            }

            if (bestIndex < 0)
                return 0;

            var bestEntryOffset = 6 + bestIndex * 16;
            var imageSize = BitConverter.ToInt32(icoData, bestEntryOffset + 8);
            var imageOffset = BitConverter.ToInt32(icoData, bestEntryOffset + 12);

            if (imageSize <= 0 || imageOffset < 0 || imageOffset + imageSize > icoData.Length)
                return 0;

            // The image data is either a DIB (BMP without the file header) or a PNG,
            // both of which are accepted directly
            var image = new byte[imageSize];
            Array.Copy(icoData, imageOffset, image, 0, imageSize);

            var handle = NativeMethods.CreateIconFromResourceEx(
                image,
                (uint)image.Length,
                true,
                IconResourceVersion,
                width,
                height,
                0
            );

            if (handle == 0)
            {
                Debug.WriteLine(
                    $"Failed to create icon from image data. Error {Marshal.GetLastWin32Error()}."
                );
            }

            return handle;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read icon data: {ex}");
            return 0;
        }
    }
}
