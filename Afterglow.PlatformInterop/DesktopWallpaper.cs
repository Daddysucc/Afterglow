using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Afterglow.PlatformInterop;

/// <summary>
/// The desktop wallpaper, set on each display separately (through the shell's IDesktopWallpaper).
/// </summary>
/// <remarks>
/// Talks to Explorer, so calls can take a moment: best made off the UI thread.
/// </remarks>
public sealed partial class DesktopWallpaper : IDisposable
{
    private static readonly Guid ClassId = new("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");
    private static readonly Guid InterfaceId = new("B92B56A9-8B55-4E14-9A89-0199BBB6F93B");

    // CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER
    private const uint ClassContext = 0x1 | 0x4;

    // DESKTOP_WALLPAPER_POSITION.DWPOS_FILL
    public const int FillPosition = 4;

    private readonly IDesktopWallpaper _wallpaper;

    private DesktopWallpaper(IDesktopWallpaper wallpaper) => _wallpaper = wallpaper;

    /// <summary>
    /// Connects to the shell's desktop wallpaper, or returns null if it isn't available (e.g. no
    /// Explorer running).
    /// </summary>
    public static DesktopWallpaper? TryCreate()
    {
        var result = CoCreateInstance(ClassId, 0, ClassContext, InterfaceId, out var pointer);
        if (result < 0 || pointer == 0)
            return null;

        try
        {
            var wallpaper = (IDesktopWallpaper)
                new StrategyBasedComWrappers().GetOrCreateObjectForComInstance(
                    pointer,
                    CreateObjectFlags.UniqueInstance
                );

            return new DesktopWallpaper(wallpaper);
        }
        catch
        {
            return null;
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    /// <summary>
    /// Displays that are connected right now: their IDs (as the shell knows them) and bounds.
    /// </summary>
    public IReadOnlyList<(string Id, Rect Bounds)> GetMonitors()
    {
        var monitors = new List<(string, Rect)>();
        var count = _wallpaper.GetMonitorDevicePathCount();

        for (var i = 0u; i < count; i++)
        {
            try
            {
                var id = _wallpaper.GetMonitorDevicePathAt(i);

                // Fails for displays that the shell remembers but aren't connected
                var bounds = _wallpaper.GetMonitorRECT(id);
                if (bounds.Right > bounds.Left && bounds.Bottom > bounds.Top)
                    monitors.Add((id, bounds));
            }
            catch (COMException) { }
        }

        return monitors;
    }

    /// <summary>
    /// The image file shown on the given display, or null if there isn't one.
    /// </summary>
    public string? TryGetWallpaper(string monitorId)
    {
        try
        {
            var path = _wallpaper.GetWallpaper(monitorId);
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        catch (COMException)
        {
            return null;
        }
    }

    public void SetWallpaper(string monitorId, string filePath) =>
        _wallpaper.SetWallpaper(monitorId, filePath);

    // How images are fitted to the displays (for all of them at once)
    public int Position
    {
        get => _wallpaper.GetPosition();
        set => _wallpaper.SetPosition(value);
    }

    public void Dispose()
    {
        if (_wallpaper is IDisposable disposable)
            disposable.Dispose();
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(
        in Guid classId,
        nint outer,
        uint classContext,
        in Guid interfaceId,
        out nint pointer
    );

    // Only the methods in use, in the interface's order (up to the last one used)
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    internal partial interface IDesktopWallpaper
    {
        void SetWallpaper(string? monitorId, string wallpaper);

        string GetWallpaper(string? monitorId);

        string GetMonitorDevicePathAt(uint monitorIndex);

        uint GetMonitorDevicePathCount();

        Rect GetMonitorRECT(string monitorId);

        void SetBackgroundColor(uint color);

        uint GetBackgroundColor();

        void SetPosition(int position);

        int GetPosition();
    }
}
