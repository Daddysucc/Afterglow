using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LightBulb.PlatformInterop.Internal;

internal static partial class NativeMethods
{
    private const string User32 = "user32.dll";

    [DllImport(User32, SetLastError = true)]
    public static extern bool EnumDisplayMonitors(
        nint hdc,
        nint lprcClip,
        EnumMonitorsProc lpfnEnum,
        nint dwData
    );

    [DllImport(User32, SetLastError = true)]
    public static extern nint MonitorFromWindow(nint hWnd, uint dwFlags);

    [DllImport(User32, SetLastError = true)]
    public static extern bool GetMonitorInfo(nint hMonitor, ref MonitorInfoEx lpmi);

    [DllImport(User32, SetLastError = true)]
    public static extern ushort RegisterClassEx(ref WndClassEx lpwcx);

    [DllImport(User32, SetLastError = true)]
    public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport(User32, SetLastError = true)]
    public static extern nint CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam
    );

    [DllImport(User32, SetLastError = true)]
    public static extern bool DestroyWindow(nint hWnd);

    [DllImport(User32, SetLastError = true)]
    public static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport(User32, SetLastError = true)]
    public static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport(User32, SetLastError = true)]
    public static extern nint GetForegroundWindow();

    [DllImport(User32, SetLastError = true)]
    public static extern bool GetWindowRect(nint hWnd, out Rect lpRect);

    [DllImport(User32, SetLastError = true)]
    public static extern bool GetClientRect(nint hWnd, out Rect lpRect);

    [DllImport(User32, SetLastError = true)]
    public static extern bool IsWindowVisible(nint hWnd);

    [DllImport(User32)]
    public static extern bool IsIconic(nint hWnd);

    [DllImport(User32, SetLastError = true)]
    public static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport(User32, SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport(User32, SetLastError = true)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport(User32, SetLastError = true)]
    public static extern bool EnumChildWindows(
        nint hWndParent,
        EnumWindowsProc lpEnumFunc,
        nint lParam
    );

    [DllImport(User32, SetLastError = true)]
    public static extern nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint hmodWinEventProc,
        WinEventProc pfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags
    );

    [DllImport(User32, SetLastError = true)]
    public static extern bool UnhookWinEvent(nint hWinEventHook);

    // Unicode, so that text in any language displays correctly
    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int MessageBox(nint hWnd, string text, string caption, uint type);

    [DllImport(User32, SetLastError = true)]
    public static extern bool AllowSetForegroundWindow(int dwProcessId);

    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLength(nint hWnd);

    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport(User32, SetLastError = true)]
    public static extern bool RegisterHotKey(nint hWnd, int id, int fsModifiers, int vk);

    [DllImport(User32, SetLastError = true)]
    public static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport(User32, SetLastError = true)]
    public static extern nint RegisterPowerSettingNotification(
        nint hRecipient,
        Guid powerSettingGuid,
        int flags
    );

    [DllImport(User32, SetLastError = true)]
    public static extern bool UnregisterPowerSettingNotification(nint handle);

    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint RegisterWindowMessage(string lpString);

    [DllImport(User32, SetLastError = true)]
    public static extern bool GetCursorPos(out NativePoint lpPoint);

    [DllImport(User32, SetLastError = true)]
    public static extern nint LoadIcon(nint hInstance, nint lpIconName);

    [DllImport(User32, SetLastError = true)]
    public static extern bool DestroyIcon(nint hIcon);

    [DllImport(User32, SetLastError = true)]
    public static extern nint OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

    [DllImport(User32, SetLastError = true)]
    public static extern bool CloseDesktop(nint hDesktop);

    [DllImport(User32)]
    public static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements
    );

    [DllImport(User32)]
    public static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [Out] DisplayConfigPathInfo[] pathArray,
        ref uint numModeInfoArrayElements,
        [Out] DisplayConfigModeInfo[] modeInfoArray,
        nint currentTopologyId
    );

    [DllImport(User32)]
    public static extern int DisplayConfigGetDeviceInfo(
        ref DisplayConfigSourceDeviceName requestPacket
    );

    [DllImport(User32)]
    public static extern int DisplayConfigGetDeviceInfo(
        ref DisplayConfigTargetDeviceName requestPacket
    );

    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool EnumDisplayDevices(
        string? lpDevice,
        uint iDevNum,
        ref DisplayDevice lpDisplayDevice,
        uint dwFlags
    );

    [DllImport(User32)]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport(User32, SetLastError = true)]
    public static extern nint CreateIconFromResourceEx(
        byte[] presbits,
        uint dwResSize,
        bool fIcon,
        uint dwVer,
        int cxDesired,
        int cyDesired,
        uint flags
    );

    [DllImport(User32)]
    public static extern nint MonitorFromPoint(NativePoint pt, uint dwFlags);

    [DllImport(User32, SetLastError = true)]
    public static extern bool SetLayeredWindowAttributes(
        nint hwnd,
        uint crKey,
        byte bAlpha,
        uint dwFlags
    );

    [DllImport(User32, SetLastError = true, EntryPoint = "GetWindowLongPtrW")]
    public static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport(User32, SetLastError = true, EntryPoint = "SetWindowLongPtrW")]
    public static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    // 32-bit processes only have the non-pointer variants
    [DllImport(User32, SetLastError = true, EntryPoint = "GetWindowLongW")]
    public static extern int GetWindowLong32(nint hWnd, int nIndex);

    [DllImport(User32, SetLastError = true, EntryPoint = "SetWindowLongW")]
    public static extern int SetWindowLong32(nint hWnd, int nIndex, int dwNewLong);

    [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool GetUserObjectInformation(
        nint hObj,
        int nIndex,
        StringBuilder? pvInfo,
        int nLength,
        out int lpnLengthNeeded
    );
}
