using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Afterglow.PlatformInterop.Internal;
using PowerKit;

namespace Afterglow.PlatformInterop;

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
    private const int NifInfo = 0x10;

    // NIIF_INFO (the standard information icon), NIIF_USER (an icon of one's own), NIIF_LARGE_ICON
    private const int NiifInfo = 0x1;
    private const int NiifUser = 0x4;
    private const int NiifLargeIcon = 0x20;

    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonUp = 0x0205;

    // NIN_BALLOONUSERCLICK (WM_USER + 5)
    private const int NinBalloonUserClick = 0x0400 + 5;

    // Must be within the WM_APP range (0x8000-0xBFFF)
    private const int CallbackMessageId = 0x8000 + 0x0142;

    private const int IconId = 1;

    // Contents of the .ico file to show, or null to use the generic application icon
    private readonly byte[]? _iconData;
    private readonly IDisposable _wndProcRegistration;

    private nint _iconHandle;
    private bool _ownsIconHandle;

    // The icon at the size notifications show it (created when the first one is shown)
    private nint _notificationIconHandle;
    private string _toolTip;
    private bool _isDisposed;

    public NotifyIcon(string toolTip, byte[]? iconData)
    {
        _toolTip = toolTip;
        _iconData = iconData;

        LoadIcon();

        // When Explorer restarts, all notification icons are lost and need to be added again.
        // The system DPI may also have changed by then, so the icon is re-created as well.
        var taskbarCreatedMessageId = NativeMethods.RegisterWindowMessage("TaskbarCreated");

        _wndProcRegistration = Disposable.Merge(
            WndProcSponge.Default.Listen(CallbackMessageId, OnCallbackMessage),
            WndProcSponge.Default.Listen(
                (int)taskbarCreatedMessageId,
                _ =>
                {
                    LoadIcon();
                    Add();
                }
            )
        );

        Add();
    }

    /// <summary>
    /// Occurs when the icon is clicked with the left mouse button.
    /// </summary>
    public event EventHandler? LeftClicked;

    /// <summary>
    /// Occurs when the icon is clicked with the right mouse button. The argument is the area
    /// to anchor a menu to, in physical screen pixels: the icon itself if its location is known,
    /// or the cursor position otherwise.
    /// </summary>
    public event EventHandler<Rect>? RightClicked;

    /// <summary>
    /// Occurs when the notification shown by <see cref="ShowNotification" /> is clicked.
    /// </summary>
    public event EventHandler? NotificationClicked;

    /// <summary>
    /// Shows a notification from the icon (a regular Windows notification, on Windows 10 and
    /// later, which also goes to the notification center). It doesn't take the focus, and
    /// Windows holds it back while the user shouldn't be disturbed (e.g. in a fullscreen game).
    /// </summary>
    public void ShowNotification(string title, string text)
    {
        if (_isDisposed)
            return;

        var data = CreateData(NifInfo);

        // (limited to 64 and 256 characters, including the null terminator)
        data.InfoTitle = title.Length > 63 ? title[..63] : title;
        data.Info = text.Length > 255 ? text[..255] : text;

        // The app's own icon at the large size (Windows doesn't show a notification at all when
        // it's asked for a large icon, but given the tray's small one), or else the standard one
        if (_notificationIconHandle == 0 && _iconData is not null)
        {
            var (width, height) = IconHandle.GetLargeIconSize();
            _notificationIconHandle = IconHandle.TryCreateFromIco(_iconData, width, height);
        }

        if (_notificationIconHandle != 0)
        {
            data.InfoFlags = NiifUser | NiifLargeIcon;
            data.BalloonIconHandle = _notificationIconHandle;
        }
        else
        {
            data.InfoFlags = NiifInfo;
        }

        if (!NativeMethods.Shell_NotifyIcon(NimModify, ref data))
        {
            Debug.WriteLine($"Failed to show a notification. Error {Marshal.GetLastWin32Error()}.");
        }
    }

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

    // Creates the icon at the system's small icon size, replacing (and destroying) the previous one
    private void LoadIcon()
    {
        var (width, height) = IconHandle.GetSmallIconSize();

        var handle = _iconData is not null
            ? IconHandle.TryCreateFromIco(_iconData, width, height)
            : 0;
        var ownsHandle = handle != 0;

        // IDI_APPLICATION (shared, must not be destroyed)
        if (handle == 0)
            handle = NativeMethods.LoadIcon(0, 32512);

        DestroyIcon();

        _iconHandle = handle;
        _ownsIconHandle = ownsHandle;
    }

    private void DestroyIcon()
    {
        if (_ownsIconHandle && _iconHandle != 0)
            NativeMethods.DestroyIcon(_iconHandle);

        _iconHandle = 0;
        _ownsIconHandle = false;
    }

    /// <summary>
    /// Gets the location of the icon, in physical screen pixels.
    /// </summary>
    public Rect? TryGetIconRect()
    {
        var identifier = new NotifyIconIdentifier
        {
            Size = Marshal.SizeOf<NotifyIconIdentifier>(),
            WindowHandle = WndProcSponge.Default.Handle,
            Id = IconId,
        };

        var result = NativeMethods.Shell_NotifyIconGetRect(ref identifier, out var rect);

        // The icon may be hidden in the overflow area, in which case there's no location to report
        return result == 0 && rect.Right > rect.Left && rect.Bottom > rect.Top ? rect : null;
    }

    private void OnCallbackMessage(WndProcMessage message)
    {
        // With the default (legacy) notification icon version, the low word of lParam is the mouse message
        switch ((int)(message.LParam & 0xFFFF))
        {
            case WmLButtonUp:
                LeftClicked?.Invoke(this, EventArgs.Empty);
                break;

            case NinBalloonUserClick:
                NotificationClicked?.Invoke(this, EventArgs.Empty);
                break;

            case WmRButtonUp:
            {
                var (x, y) = GetCursorPosition();
                RightClicked?.Invoke(this, TryGetIconRect() ?? new Rect(x, y, x, y));
                break;
            }
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
        DestroyIcon();

        if (_notificationIconHandle != 0)
            NativeMethods.DestroyIcon(_notificationIconHandle);
        _notificationIconHandle = 0;

        GC.SuppressFinalize(this);
    }
}

public partial class NotifyIcon
{
    /// <summary>
    /// Gets the current cursor position, in physical screen coordinates.
    /// </summary>
    public static (int X, int Y) GetCursorPosition() =>
        NativeMethods.GetCursorPos(out var point) ? (point.X, point.Y) : (0, 0);
}
