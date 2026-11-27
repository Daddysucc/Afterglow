using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Afterglow.PlatformInterop;

/// <summary>
/// The Windows accent color (as used by Windows Dynamic Lighting's "Match my Windows accent
/// color"), set for the current user.
/// </summary>
/// <remarks>
/// Windows has no documented API for setting it. It's set through the one that the Settings app
/// itself goes through (uxtheme's SetUserColorPreference), which applies it right away and works
/// out its shades. Only if that isn't there, it's written the way Settings stores it and then
/// announced to every window, which is much slower (every window handles it in turn) and which
/// Windows may not pick up.
/// </remarks>
public static partial class AccentColor
{
    private const string AccentKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";

    private const string DwmKey = @"Software\Microsoft\Windows\DWM";

    private const string DesktopKey = @"Control Panel\Desktop";

    // Every value that's changed, so that they can all be put back
    private static readonly (string Key, string Name)[] Values =
    [
        (AccentKey, "AccentPalette"),
        (AccentKey, "AccentColorMenu"),
        (AccentKey, "StartColorMenu"),
        (DwmKey, "AccentColor"),
        (DwmKey, "ColorizationColor"),
        (DwmKey, "ColorizationAfterglow"),
        (DesktopKey, "AutoColorization"),
    ];

    /// <summary>
    /// Sets the accent color (with "Automatic" turned off, as Windows would otherwise pick it from
    /// the wallpaper again).
    /// </summary>
    public static void Set(byte red, byte green, byte blue)
    {
        static uint ToAbgr(byte r, byte g, byte b) =>
            0xFF000000u | ((uint)b << 16) | ((uint)g << 8) | r;

        using (var desktop = Registry.CurrentUser.CreateSubKey(DesktopKey))
        {
            if (desktop.GetValue("AutoColorization") is not 0)
                desktop.SetValue("AutoColorization", 0, RegistryValueKind.DWord);
        }

        // (the Start menu's color is a darker shade, as Windows picks it)
        if (
            TrySetPreference(
                ToAbgr((byte)(red * 0.8), (byte)(green * 0.8), (byte)(blue * 0.8)),
                ToAbgr(red, green, blue)
            )
        )
            return;

        static (byte, byte, byte) Shade(byte r, byte g, byte b, double factor) =>
            factor >= 1
                ? (
                    (byte)(r + (255 - r) * (factor - 1)),
                    (byte)(g + (255 - g) * (factor - 1)),
                    (byte)(b + (255 - b) * (factor - 1))
                )
                : ((byte)(r * factor), (byte)(g * factor), (byte)(b * factor));

        // Three lighter shades, the accent itself, three darker ones, and an unused entry
        var palette = new List<byte>(32);
        foreach (var factor in new[] { 1.6, 1.4, 1.2, 1, 0.8, 0.6, 0.4, 0.4 })
        {
            var (r, g, b) = Shade(red, green, blue, factor);
            palette.AddRange([r, g, b, 0]);
        }

        var (darkR, darkG, darkB) = Shade(red, green, blue, 0.8);

        using (var accent = Registry.CurrentUser.CreateSubKey(AccentKey))
        {
            accent.SetValue("AccentPalette", palette.ToArray(), RegistryValueKind.Binary);
            accent.SetValue(
                "AccentColorMenu",
                unchecked((int)ToAbgr(red, green, blue)),
                RegistryValueKind.DWord
            );
            accent.SetValue(
                "StartColorMenu",
                unchecked((int)ToAbgr(darkR, darkG, darkB)),
                RegistryValueKind.DWord
            );
        }

        using (var dwm = Registry.CurrentUser.CreateSubKey(DwmKey))
        {
            var argb = unchecked(
                (int)(0xC4000000u | ((uint)red << 16) | ((uint)green << 8) | blue)
            );

            dwm.SetValue(
                "AccentColor",
                unchecked((int)ToAbgr(red, green, blue)),
                RegistryValueKind.DWord
            );
            dwm.SetValue("ColorizationColor", argb, RegistryValueKind.DWord);
            dwm.SetValue("ColorizationAfterglow", argb, RegistryValueKind.DWord);
        }

        Announce();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImmersiveColorPreference
    {
        public uint StartColor;
        public uint AccentColor;
    }

    [DllImport("uxtheme.dll", EntryPoint = "#122")]
    private static extern int SetUserColorPreference(
        ref ImmersiveColorPreference preference,
        bool isCommitted
    );

    // Sets the accent (and Start menu) color as Settings does, if Windows has the way to
    private static bool TrySetPreference(uint startColor, uint accentColor)
    {
        try
        {
            var preference = new ImmersiveColorPreference
            {
                StartColor = startColor,
                AccentColor = accentColor,
            };

            return SetUserColorPreference(ref preference, true) >= 0;
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Takes a snapshot of the accent color settings, as text (to be put back with
    /// <see cref="Restore" />).
    /// </summary>
    public static string Save()
    {
        var lines = new List<string>();

        foreach (var (keyPath, name) in Values)
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            var value = key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);

            var encoded = value switch
            {
                byte[] bytes => "binary:" + Convert.ToBase64String(bytes),
                int number => "dword:" + number.ToString(CultureInfo.InvariantCulture),
                string text => "string:" + text,
                _ => "none:",
            };

            lines.Add($"{keyPath}|{name}|{encoded}");
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Puts back the accent color settings from a snapshot taken by <see cref="Save" />.
    /// </summary>
    public static void Restore(string snapshot)
    {
        foreach (var line in snapshot.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|', 3);
            if (parts.Length != 3 || !Values.Contains((parts[0], parts[1])))
                continue;

            var (keyPath, name, encoded) = (parts[0], parts[1], parts[2]);
            var separator = encoded.IndexOf(':');
            if (separator < 0)
                continue;

            var (kind, data) = (encoded[..separator], encoded[(separator + 1)..]);

            using var key = Registry.CurrentUser.CreateSubKey(keyPath);
            switch (kind)
            {
                case "binary":
                    key.SetValue(name, Convert.FromBase64String(data), RegistryValueKind.Binary);
                    break;
                case "dword":
                    key.SetValue(
                        name,
                        int.Parse(data, CultureInfo.InvariantCulture),
                        RegistryValueKind.DWord
                    );
                    break;
                case "string":
                    key.SetValue(name, data, RegistryValueKind.String);
                    break;
                default:
                    key.DeleteValue(name, false);
                    break;
            }
        }

        // Applied as Settings would (or else announced)
        using var accent = Registry.CurrentUser.OpenSubKey(AccentKey);
        if (
            accent?.GetValue("StartColorMenu") is int startColor
            && accent.GetValue("AccentColorMenu") is int accentColor
            && TrySetPreference(unchecked((uint)startColor), unchecked((uint)accentColor))
        )
            return;

        Announce();
    }

    // Tells running applications (Explorer, Dynamic Lighting, title bars) that the colors changed
    private static void Announce()
    {
        const nint broadcast = 0xFFFF;
        const uint settingChange = 0x001A;
        const uint abortIfHung = 0x0002;

        var text = Marshal.StringToHGlobalUni("ImmersiveColorSet");
        try
        {
            SendMessageTimeout(broadcast, settingChange, 0, text, abortIfHung, 1000, out _);
        }
        finally
        {
            Marshal.FreeHGlobal(text);
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
    private static partial nint SendMessageTimeout(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam,
        uint flags,
        uint timeout,
        out nint result
    );
}
