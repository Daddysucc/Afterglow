using System;
using System.Runtime.InteropServices;

namespace Afterglow.PlatformInterop.Internal;

// POWERBROADCAST_SETTING, with its data read as one 32-bit value (which every setting that's
// listened to has, e.g. the display's state)
[StructLayout(LayoutKind.Sequential)]
internal readonly record struct PowerBroadcastSetting(
    Guid PowerSettingId,
    uint DataLength,
    uint Data
);
