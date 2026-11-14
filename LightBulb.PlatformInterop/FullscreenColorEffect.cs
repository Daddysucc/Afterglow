using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using LightBulb.PlatformInterop.Internal;

namespace LightBulb.PlatformInterop;

/// <summary>
/// Color transformation of the whole screen (all displays), using the Windows Magnification API.
/// It's applied by the desktop compositor, so it's independent of (and combines with) gamma ramps.
/// </summary>
public static class FullscreenColorEffect
{
    // BT.709 luma coefficients
    private const float RedLuminance = 0.2126f;
    private const float GreenLuminance = 0.7152f;
    private const float BlueLuminance = 0.0722f;

    /// <summary>
    /// Desaturates the screen: each output color channel gets the luminance of the input color.
    /// </summary>
    // csharpier-ignore
    public static IReadOnlyList<float> GrayscaleMatrix { get; } =
    [
        RedLuminance, RedLuminance, RedLuminance, 0, 0,
        GreenLuminance, GreenLuminance, GreenLuminance, 0, 0,
        BlueLuminance, BlueLuminance, BlueLuminance, 0, 0,
        0, 0, 0, 1, 0,
        0, 0, 0, 0, 1,
    ];

    /// <summary>
    /// Leaves the screen unchanged.
    /// </summary>
    // csharpier-ignore
    public static IReadOnlyList<float> IdentityMatrix { get; } =
    [
        1, 0, 0, 0, 0,
        0, 1, 0, 0, 0,
        0, 0, 1, 0, 0,
        0, 0, 0, 1, 0,
        0, 0, 0, 0, 1,
    ];

    private static readonly Lock Lock = new();
    private static bool _isInitialized;

    /// <summary>
    /// Applies the given 5x5 matrix to the whole screen, initializing the Magnification API first
    /// if needed. Returns false if that failed (e.g. the API isn't available on this system).
    /// </summary>
    public static bool TryApply(IReadOnlyList<float> matrix)
    {
        if (matrix.Count != 25)
            throw new ArgumentException("The matrix must have 25 (5x5) elements.", nameof(matrix));

        using (Lock.EnterScope())
        {
            try
            {
                if (!_isInitialized)
                {
                    if (!NativeMethods.MagInitialize())
                    {
                        Debug.WriteLine(
                            $"Failed to initialize magnification. Error {Marshal.GetLastWin32Error()}."
                        );

                        return false;
                    }

                    _isInitialized = true;
                }

                var effect = new MagColorEffect { Transform = matrix.ToArray() };
                if (!NativeMethods.MagSetFullscreenColorEffect(ref effect))
                {
                    Debug.WriteLine(
                        $"Failed to set the fullscreen color effect. Error {Marshal.GetLastWin32Error()}."
                    );

                    return false;
                }

                return true;
            }
            // Magnification.dll is missing on some editions of Windows (e.g. Server Core)
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                Debug.WriteLine($"Magnification API is not available: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Restores the screen to normal and releases the Magnification API. Safe to call at any
    /// time, any number of times, including when nothing has been applied.
    /// </summary>
    public static void Reset()
    {
        using (Lock.EnterScope())
        {
            if (!_isInitialized)
                return;

            try
            {
                var effect = new MagColorEffect { Transform = IdentityMatrix.ToArray() };
                NativeMethods.MagSetFullscreenColorEffect(ref effect);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to reset the fullscreen color effect: {ex.Message}");
            }
            finally
            {
                try
                {
                    NativeMethods.MagUninitialize();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to uninitialize magnification: {ex.Message}");
                }

                _isInitialized = false;
            }
        }
    }
}
