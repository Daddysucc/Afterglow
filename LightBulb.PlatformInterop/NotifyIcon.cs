using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LightBulb.PlatformInterop.Internal;
using PowerKit;

namespace LightBulb.PlatformInterop;

/// <summary>
/// Icon in the notification area of the taskbar (system tray).
/// Unlike the built-in tray icon support in UI frameworks, this exposes right-clicks,
/// so that a custom menu can be shown instead of the native one.
/// </summary>
public partial class NotifyIcon : IDisposable
{
    private const int NimAdd = 0;
    private const int NimModify = 1;
    private const int NimDelete = 2;

    private const int NifMessage = 0x1;
    private const int NifIcon = 0x2;
    private const int NifTip = 0x4;

    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonUp = 0x0205;

    // WM_APP + arbitrary offset
    private const int CallbackMessageId = 0x8000 + 0x4C42;

    private const int IconId = 1;

    private readonly nint _iconHandle;
    private readonly bool _ownsIconHandle;
    private readonly IDisposable _wndProcRegistration;

    private string _toolTip;
    private bool _isDisposed;

    public NotifyIcon(string toolTip)
    {
        _toolTip = toolTip;
        (_iconHandle, _ownsIconHandle) = LoadApplicationIcon();

        // Re-add the icon when Explorer restarts, as all notification icons are lost then
        var taskbarCreatedMessageId = NativeMethods.RegisterWindowMessage("TaskbarCreated");

        _wndProcRegistration = Disposable.Merge(
            WndProcSponge.Default.Listen(CallbackMessageId, OnCallbackMessage),
            WndProcSponge.Default.Listen((int)taskbarCreatedMessageId, _ => Add())
        );

        Add();
    }

    /// <summary>
    /// Occurs when the icon is clicked with the left mouse button.
    /// </summary>
    public event EventHandler? LeftClicked;

    /// <summary>
    /// Occurs when the icon is clicked with the right mouse button.
    /// The argument is the cursor position, in physical screen coordinates.
    /// </summary>
    public event EventHandler<(int X, int Y)>? RightClicked;

    public string ToolTip
    {
        get => _toolTip;
        set
        {
            if (string.Equals(_toolTip, value, StringComparison.Ordinal))
                return;

            _toolTip = value;
            Update(NimModify, NifTip);
        }
    }

    private void OnCallbackMessage(WndProcMessage message)
    {
        // With the default (legacy) notification icon version, the low word of lParam is the mouse message
        switch ((int)(message.LParam & 0xFFFF))
        {
            case WmLButtonUp:
                LeftClicked?.Invoke(this, EventArgs.Empty);
                break;

            case WmRButtonUp:
                RightClicked?.Invoke(this, GetCursorPosition());
                break;
        }
    }

    private NotifyIconData CreateData(int flags) =>
        new()
        {
            Size = Marshal.SizeOf<NotifyIconData>(),
            WindowHandle = WndProcSponge.Default.Handle,
            Id = IconId,
            Flags = flags,
            CallbackMessage = CallbackMessageId,
            IconHandle = _iconHandle,
            // Tooltips are limited to 128 characters, including the null terminator
            ToolTip = _toolTip.Length > 127 ? _toolTip[..127] : _toolTip,
            Info = "",
            InfoTitle = "",
        };

    private void Update(int message, int flags)
    {
        if (_isDisposed)
            return;

        var data = CreateData(flags);
        if (!NativeMethods.Shell_NotifyIcon(message, ref data))
        {
            Debug.WriteLine(
                $"Failed to update notification icon (message {message}). "
                    + $"Error {Marshal.GetLastWin32Error()}."
            );
        }
    }

    private void Add() => Update(NimAdd, NifMessage | NifIcon | NifTip);

    public void Dispose()
    {
        if (_isDisposed)
            return;

        Update(NimDelete, 0);
        _isDisposed = true;

        _wndProcRegistration.Dispose();

        if (_ownsIconHandle)
            NativeMethods.DestroyIcon(_iconHandle);

        GC.SuppressFinalize(this);
    }
}

public partial class NotifyIcon
{
    // Use the icon embedded in the executable, falling back to the generic application icon
    private static (nint Handle, bool IsOwned) LoadApplicationIcon()
    {
        var executableFilePath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executableFilePath))
        {
            var smallIcons = new nint[1];
            if (
                NativeMethods.ExtractIconEx(executableFilePath, 0, null, smallIcons, 1) > 0
                && smallIcons[0] != 0
            )
            {
                return (smallIcons[0], true);
            }
        }

        // IDI_APPLICATION
        return (NativeMethods.LoadIcon(0, 32512), false);
    }

    /// <summary>
    /// Gets the current cursor position, in physical screen coordinates.
    /// </summary>
    public static (int X, int Y) GetCursorPosition() =>
        NativeMethods.GetCursorPos(out var point) ? (point.X, point.Y) : (0, 0);
}
