using System.ComponentModel;
using System.Runtime.InteropServices;
using StickyDock.Core.Services;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Native Win32 <c>Shell_NotifyIconW</c> implementation of <see cref="ITrayIconService"/>
/// (ADR-006, §4.6-01, HU05 CA #1, A2, E6-CA1, E6-CA2).
/// </summary>
public sealed class Win32TrayIconService : ITrayIconService
{
    /// <summary>
    /// Base user-defined application window message (<c>WM_APP</c>).
    /// </summary>
    public const uint WmApp = 0x8000;

    /// <summary>
    /// Tray callback message (<c>WM_APP + 1</c>).
    /// </summary>
    public const uint WmTrayIcon = WmApp + 1;

    /// <summary>
    /// Left mouse button released (<c>WM_LBUTTONUP</c>).
    /// </summary>
    public const uint WmLButtonUp = 0x0202;

    /// <summary>
    /// Right mouse button released (<c>WM_RBUTTONUP</c>).
    /// </summary>
    public const uint WmRButtonUp = 0x0205;

    /// <summary>
    /// Native menu command identifier for opening Settings.
    /// </summary>
    public const uint SettingsMenuCommandId = 1;

    /// <summary>
    /// Native menu command identifier for exiting the application.
    /// </summary>
    public const uint ExitMenuCommandId = 2;

    private const uint WmNull = 0x0000;
    private const uint NimAdd = 0x00000000;
    private const uint NimDelete = 0x00000002;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const nint IdiApplication = 32512;
    private const uint MfString = 0x00000000;
    private const uint MfSeparator = 0x00000800;
    private const uint TpmRightButton = 0x0002;
    private const uint TpmReturnCmd = 0x0100;
    private const uint TrayIconId = 1;
    private const string TooltipText = "Sticky Dock Utility";

    private static readonly string[] s_menuItemTexts = ["Settings", "Exit"];

    private readonly string _windowClassName;
    private readonly nint _moduleHandle;
    private readonly WndProcDelegate _wndProcDelegate;
    private nint _messageWindowHandle;
    private bool _classRegistered;
    private bool _addedToTray;
    private bool _isVisible;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32TrayIconService"/> class.
    /// </summary>
    public Win32TrayIconService()
    {
        _windowClassName = $"StickyDockTray_{Guid.NewGuid():N}";
        _moduleHandle = NativeMethods.GetModuleHandleW(null);
        _wndProcDelegate = WndProc;

        CreateMessageWindow();
    }

    /// <inheritdoc />
    public event EventHandler? SettingsRequested;

    /// <inheritdoc />
    public event EventHandler? ExitRequested;

    /// <summary>
    /// Gets a value indicating whether the tray icon is currently visible.
    /// </summary>
    public bool IsVisible => !_disposed && _isVisible;

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the context menu item texts in display order.
    /// </summary>
    public IReadOnlyList<string> MenuItemTexts => s_menuItemTexts;

    /// <inheritdoc />
    public void Show()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isVisible)
        {
            return;
        }

        var iconHandle = NativeMethods.LoadIconW(nint.Zero, IdiApplication);
        var notifyData = CreateNotifyIconData(NifMessage | NifIcon | NifTip, iconHandle);

        // Shell_NotifyIconW returns false when no taskbar shell exists (e.g. headless CI or early startup);
        // keep the service operational instead of crashing the app.
        _addedToTray = NativeMethods.Shell_NotifyIconW(NimAdd, ref notifyData);
        _isVisible = true;
    }

    /// <summary>
    /// Simulates receiving a tray callback mouse message for automated testing.
    /// </summary>
    /// <param name="mouseMessage">Mouse message identifier (such as <see cref="WmLButtonUp"/> or <see cref="WmRButtonUp"/>).</param>
    /// <param name="menuCommandSelector">
    /// Optional selector invoked instead of blocking on the native popup menu when <paramref name="mouseMessage"/>
    /// is <see cref="WmRButtonUp"/>.
    /// </param>
    public void SimulateTrayMessage(uint mouseMessage, Func<uint>? menuCommandSelector = null)
    {
        if (_disposed)
        {
            return;
        }

        if (mouseMessage == WmRButtonUp && menuCommandSelector is not null)
        {
            DispatchMenuCommand(menuCommandSelector());
            return;
        }

        HandleTrayMessage(mouseMessage);
    }

    /// <summary>
    /// Simulates selecting a native context menu command identifier for automated testing.
    /// </summary>
    /// <param name="commandId">Menu command identifier (<see cref="SettingsMenuCommandId"/> or <see cref="ExitMenuCommandId"/>).</param>
    public void SimulateMenuCommand(uint commandId)
    {
        if (_disposed)
        {
            return;
        }

        DispatchMenuCommand(commandId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_addedToTray && _messageWindowHandle != nint.Zero)
        {
            var notifyData = CreateNotifyIconData(0, nint.Zero);
            NativeMethods.Shell_NotifyIconW(NimDelete, ref notifyData);
        }

        _addedToTray = false;
        _isVisible = false;

        if (_messageWindowHandle != nint.Zero)
        {
            NativeMethods.DestroyWindow(_messageWindowHandle);
            _messageWindowHandle = nint.Zero;
        }

        if (_classRegistered)
        {
            NativeMethods.UnregisterClassW(_windowClassName, _moduleHandle);
            _classRegistered = false;
        }

        SettingsRequested = null;
        ExitRequested = null;
    }

    private unsafe void CreateMessageWindow()
    {
        var wndProcPointer = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);

        fixed (char* classNamePointer = _windowClassName)
        {
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = wndProcPointer,
                hInstance = _moduleHandle,
                lpszClassName = (nint)classNamePointer,
            };

            if (NativeMethods.RegisterClassExW(ref windowClass) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "RegisterClassExW failed for tray message window.");
            }
        }

        _classRegistered = true;

        _messageWindowHandle = NativeMethods.CreateWindowExW(
            dwExStyle: 0,
            lpClassName: _windowClassName,
            lpWindowName: "StickyDockTrayWindow",
            dwStyle: 0,
            x: 0,
            y: 0,
            nWidth: 0,
            nHeight: 0,
            hWndParent: new nint(-3), // HWND_MESSAGE: pure message-only window without visual/DWM participation.
            hMenu: nint.Zero,
            hInstance: _moduleHandle,
            lpParam: nint.Zero);

        if (_messageWindowHandle == nint.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            NativeMethods.UnregisterClassW(_windowClassName, _moduleHandle);
            _classRegistered = false;
            throw new Win32Exception(error, "CreateWindowExW failed for tray message window.");
        }
    }

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WmTrayIcon && !_disposed)
        {
            var mouseMessage = unchecked((uint)((ulong)lParam & 0xFFFF));
            HandleTrayMessage(mouseMessage);
            return nint.Zero;
        }

        return NativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void HandleTrayMessage(uint mouseMessage)
    {
        if (mouseMessage == WmLButtonUp)
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (mouseMessage == WmRButtonUp)
        {
            var commandId = ShowNativeContextMenu();
            DispatchMenuCommand(commandId);
        }
    }

    private uint ShowNativeContextMenu()
    {
        if (_messageWindowHandle == nint.Zero)
        {
            return 0;
        }

        NativeMethods.SetForegroundWindow(_messageWindowHandle);

        var packedPosition = NativeMethods.GetMessagePos();
        var x = unchecked((short)(packedPosition & 0xFFFF));
        var y = unchecked((short)((packedPosition >> 16) & 0xFFFF));

        var menuHandle = NativeMethods.CreatePopupMenu();
        if (menuHandle == nint.Zero)
        {
            return 0;
        }

        try
        {
            NativeMethods.AppendMenuW(menuHandle, MfString, SettingsMenuCommandId, s_menuItemTexts[0]);
            NativeMethods.AppendMenuW(menuHandle, MfSeparator, 0, null);
            NativeMethods.AppendMenuW(menuHandle, MfString, ExitMenuCommandId, s_menuItemTexts[1]);

            var command = NativeMethods.TrackPopupMenuEx(
                menuHandle,
                TpmReturnCmd | TpmRightButton,
                x,
                y,
                _messageWindowHandle,
                nint.Zero);

            NativeMethods.PostMessageW(_messageWindowHandle, WmNull, nint.Zero, nint.Zero);

            return command > 0 ? (uint)command : 0;
        }
        finally
        {
            NativeMethods.DestroyMenu(menuHandle);
        }
    }

    private void DispatchMenuCommand(uint commandId)
    {
        if (_disposed)
        {
            return;
        }

        if (commandId == SettingsMenuCommandId)
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (commandId == ExitMenuCommandId)
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private unsafe NOTIFYICONDATAW CreateNotifyIconData(uint flags, nint iconHandle)
    {
        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            hWnd = _messageWindowHandle,
            uID = TrayIconId,
            uFlags = flags,
            uCallbackMessage = WmTrayIcon,
            hIcon = iconHandle,
        };

        if ((flags & NifTip) != 0)
        {
            CopyTooltip(data.szTip, 128, TooltipText);
        }

        return data;
    }

    private static unsafe void CopyTooltip(char* destination, int capacity, string text)
    {
        var length = Math.Min(text.Length, capacity - 1);
        text.AsSpan(0, length).CopyTo(new Span<char>(destination, length));
        destination[length] = '\0';
    }
}
