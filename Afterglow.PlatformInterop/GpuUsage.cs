using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Afterglow.PlatformInterop;

/// <summary>
/// How busy the GPU is drawing (3D) and playing video, across all its engines and applications,
/// as Task Manager shows it. Something on the screen that's moving (a game, even in a window, or
/// a video) keeps it busy, while a still desktop barely uses it.
/// </summary>
/// <remarks>
/// Reading it takes about a millisecond, but starting to measure takes a few hundred (longer on
/// slower computers), so it shouldn't be used on the UI thread. It may be used from any thread.
/// </remarks>
public sealed partial class GpuUsage : IDisposable
{
    // PDH_FMT_DOUBLE
    private const uint DoubleFormat = 0x200;

    // PDH_CSTATUS_VALID_DATA, PDH_CSTATUS_NEW_DATA
    private const uint ValidData = 0;
    private const uint NewData = 1;

    private readonly Lock _lock = new();
    private nint _query;
    private nint _counter;
    private bool _isStarted;
    private bool _hasSample;
    private bool _isDisposed;

    /// <summary>
    /// Whether it can be measured at all (it can't on some virtual machines, or with performance
    /// counters turned off). Only known after the first <see cref="TryGetUsage" />.
    /// </summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>
    /// The average usage (in percent, added up over the engines) since the previous call, or
    /// <c>null</c> on the first call (which starts measuring), or if it couldn't be read.
    /// </summary>
    public double? TryGetUsage()
    {
        using (_lock.EnterScope())
        {
            if (_isDisposed || !IsAvailable)
                return null;

            if (!_isStarted)
            {
                _isStarted = true;
                IsAvailable = TryStart();
                if (!IsAvailable)
                    return null;
            }

            if (PdhCollectQueryData(_query) != 0)
                return null;

            // (the counter is a rate, which needs two samples)
            if (!_hasSample)
            {
                _hasSample = true;
                return null;
            }

            return TryReadTotal();
        }
    }

    private bool TryStart()
    {
        try
        {
            if (PdhOpenQuery(null, 0, out _query) != 0)
                return false;

            if (
                PdhAddEnglishCounter(
                    _query,
                    @"\GPU Engine(*)\Utilization Percentage",
                    0,
                    out _counter
                ) != 0
            )
            {
                PdhCloseQuery(_query);
                _query = 0;
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    private unsafe double? TryReadTotal()
    {
        uint size = 0;
        PdhGetFormattedCounterArray(_counter, DoubleFormat, ref size, out _, null);
        if (size == 0)
            return null;

        var buffer = new byte[size];
        fixed (byte* items = buffer)
        {
            if (
                PdhGetFormattedCounterArray(_counter, DoubleFormat, ref size, out var count, items)
                != 0
            )
                return null;

            var total = 0.0;
            for (var i = 0; i < count; i++)
            {
                var item = (CounterValueItem*)items + i;
                if (item->Status is not (ValidData or NewData))
                    continue;

                // Instances are named like "pid_1234_luid_..._phys_0_eng_0_engtype_3D"
                var name = new string(item->Name);
                if (
                    name.EndsWith("engtype_3D", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith("engtype_VideoDecode", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith("engtype_VideoProcessing", StringComparison.OrdinalIgnoreCase)
                )
                {
                    total += item->Value;
                }
            }

            return total;
        }
    }

    public void Dispose()
    {
        using (_lock.EnterScope())
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_query != 0 && PdhCloseQuery(_query) != 0)
                Debug.WriteLine("Failed to close the GPU usage query.");
        }
    }

    // PDH_FMT_COUNTERVALUE_ITEM_W
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct CounterValueItem
    {
        public char* Name;
        public uint Status;
        public double Value;
    }

    [LibraryImport(
        "pdh.dll",
        EntryPoint = "PdhOpenQueryW",
        StringMarshalling = StringMarshalling.Utf16
    )]
    private static partial uint PdhOpenQuery(string? dataSource, nint userData, out nint query);

    [LibraryImport(
        "pdh.dll",
        EntryPoint = "PdhAddEnglishCounterW",
        StringMarshalling = StringMarshalling.Utf16
    )]
    private static partial uint PdhAddEnglishCounter(
        nint query,
        string counterPath,
        nint userData,
        out nint counter
    );

    [LibraryImport("pdh.dll")]
    private static partial uint PdhCollectQueryData(nint query);

    [LibraryImport("pdh.dll", EntryPoint = "PdhGetFormattedCounterArrayW")]
    private static unsafe partial uint PdhGetFormattedCounterArray(
        nint counter,
        uint format,
        ref uint bufferSize,
        out uint itemCount,
        byte* itemBuffer
    );

    [LibraryImport("pdh.dll")]
    private static partial uint PdhCloseQuery(nint query);
}
