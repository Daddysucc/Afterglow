using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

public partial class DeviceContext(nint handle) : NativeResource(handle)
{
    private const int RampSize = 256;

    private int _gammaChannelOffset;

    // Reused between updates rather than allocated each time. Taken out while in use, so that a
    // concurrent call (e.g. resetting gamma from another thread while the app is crashing) gets
    // its own buffer instead of sharing one that's being filled in.
    private ushort[]? _rampBuffer;

    private bool SetGammaRamp(ushort[] ramp)
    {
        if (NativeMethods.SetDeviceGammaRamp(Handle, ramp))
            return true;

        Debug.WriteLine(
            $"Failed to set gamma ramp on device context #{Handle}. "
                + $"Error {Marshal.GetLastWin32Error()}."
        );

        return false;
    }

    // Returns false if the driver rejected the ramp (e.g. a display that doesn't support gamma ramps,
    // or one that isn't ready yet after waking up), so that the caller can try again later
    public bool SetGamma(double redMultiplier, double greenMultiplier, double blueMultiplier)
    {
        var ramp = Interlocked.Exchange(ref _rampBuffer, null) ?? new ushort[RampSize * 3];

        try
        {
            // Some drivers will ignore requests to change gamma if the specified ramp is the same as last time,
            // even if the actual gamma has been changed in-between (for example, by screen going to sleep).
            // In order to work around this, we add a small random deviation to each ramp to make sure
            // they're always unique, forcing the drivers to refresh the device context every time.
            _gammaChannelOffset = ++_gammaChannelOffset % 5;

            FillRamp(ramp, redMultiplier, greenMultiplier, blueMultiplier, _gammaChannelOffset);
            return SetGammaRamp(ramp);
        }
        finally
        {
            _rampBuffer = ramp;
        }
    }

    // Fills in the red, green, and blue ramps (in that order, one after another) as linear ramps,
    // with the given deviation added to the last value of each
    internal static void FillRamp(
        ushort[] ramp,
        double redMultiplier,
        double greenMultiplier,
        double blueMultiplier,
        int deviation
    )
    {
        for (var i = 0; i < RampSize; i++)
        {
            ramp[i] = (ushort)(i * 255 * redMultiplier);
            ramp[RampSize + i] = (ushort)(i * 255 * greenMultiplier);
            ramp[RampSize * 2 + i] = (ushort)(i * 255 * blueMultiplier);
        }

        ramp[RampSize - 1] = (ushort)(ramp[RampSize - 1] + deviation);
        ramp[RampSize * 2 - 1] = (ushort)(ramp[RampSize * 2 - 1] + deviation);
        ramp[RampSize * 3 - 1] = (ushort)(ramp[RampSize * 3 - 1] + deviation);
    }

    public bool ResetGamma() => SetGamma(1, 1, 1);

    protected override void Dispose(bool disposing)
    {
        // Don't reset gamma during dispose because this method is also called whenever
        // the device context gets invalidated.
        // Resetting gamma in such cases will cause unwanted flickering.
        // https://github.com/Tyrrrz/LightBulb/issues/206
        if (!NativeMethods.DeleteDC(Handle))
        {
            Debug.WriteLine(
                $"Failed to dispose device context #{Handle}. "
                    + $"Error {Marshal.GetLastWin32Error()}."
            );
        }
    }
}

public partial class DeviceContext
{
    public static DeviceContext? TryCreate(string deviceName)
    {
        var handle = NativeMethods.CreateDC(deviceName, deviceName, null, 0);
        if (handle == 0)
        {
            Debug.WriteLine(
                $"Failed to retrieve device context for '{deviceName}'. "
                    + $"Error {Marshal.GetLastWin32Error()}."
            );
            return null;
        }

        return new DeviceContext(handle);
    }
}
