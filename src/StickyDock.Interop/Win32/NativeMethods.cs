using System.Runtime.InteropServices;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Raw P/Invoke declarations. They are internal on purpose: the rest of the product uses the safe wrappers of this
/// assembly and never handles native types (AGENTS.md).
/// </summary>
internal static partial class NativeMethods
{
    private const string User32LibraryName = "user32.dll";
    private const string Shell32LibraryName = "shell32.dll";
    private const string Kernel32LibraryName = "kernel32.dll";
    private const string ShcoreLibraryName = "shcore.dll";

    [LibraryImport(User32LibraryName, EntryPoint = "GetSystemMetrics")]
    internal static partial int GetSystemMetrics(int index);

    [LibraryImport(User32LibraryName, EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

    [LibraryImport(User32LibraryName, EntryPoint = "MonitorFromWindow")]
    internal static partial nint MonitorFromWindow(nint hWnd, uint dwFlags);

    [LibraryImport(User32LibraryName, EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfoW(nint hMonitor, ref MONITORINFO lpmi);

    [LibraryImport(User32LibraryName, EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfoExW(nint hMonitor, ref MONITORINFOEXW lpmi);

    [LibraryImport(User32LibraryName, EntryPoint = "EnumDisplayMonitors")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [LibraryImport(ShcoreLibraryName, EntryPoint = "GetDpiForMonitor")]
    internal static partial int GetDpiForMonitor(nint hMonitor, int dpiType, out uint dpiX, out uint dpiY);

    [LibraryImport(User32LibraryName, EntryPoint = "GetDpiForWindow")]
    internal static partial uint GetDpiForWindow(nint hWnd);

    [LibraryImport(User32LibraryName, EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint FindWindowW(string? lpClassName, string? lpWindowName);

    [LibraryImport(User32LibraryName, EntryPoint = "GetWindowThreadProcessId")]
    internal static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [LibraryImport(User32LibraryName, EntryPoint = "SetWinEventHook")]
    internal static partial nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [LibraryImport(User32LibraryName, EntryPoint = "UnhookWinEvent")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnhookWinEvent(nint hWinEventHook);

    [LibraryImport(User32LibraryName, EntryPoint = "SetWindowPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [LibraryImport(User32LibraryName, EntryPoint = "SendMessageW")]
    internal static partial nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport(User32LibraryName, EntryPoint = "ReleaseCapture")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ReleaseCapture();

    [LibraryImport(Shell32LibraryName, EntryPoint = "SHAppBarMessage")]
    internal static partial nint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    [LibraryImport(Shell32LibraryName, EntryPoint = "ExtractIconExW", StringMarshalling = StringMarshalling.Utf16)]
    internal static unsafe partial uint ExtractIconExW(
        string lpszFile,
        int nIconIndex,
        nint* phiconLarge,
        nint* phiconSmall,
        uint nIcons);

    [LibraryImport(Shell32LibraryName, EntryPoint = "SHGetFileInfoW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint SHGetFileInfoW(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFOW psfi,
        uint cbFileInfo,
        uint uFlags);

    [LibraryImport(User32LibraryName, EntryPoint = "DestroyIcon")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(nint hIcon);

    [LibraryImport(Kernel32LibraryName, EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint GetModuleHandleW(string? lpModuleName);

    [LibraryImport(User32LibraryName, EntryPoint = "LoadIconW")]
    internal static partial nint LoadIconW(nint hInstance, nint lpIconName);

    [LibraryImport(User32LibraryName, EntryPoint = "RegisterClassExW")]
    internal static partial ushort RegisterClassExW(ref WNDCLASSEXW lpwcx);

    [LibraryImport(User32LibraryName, EntryPoint = "UnregisterClassW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnregisterClassW(string lpClassName, nint hInstance);

    [LibraryImport(User32LibraryName, EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint CreateWindowExW(
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
        nint lpParam);

    [LibraryImport(User32LibraryName, EntryPoint = "DestroyWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyWindow(nint hWnd);

    [LibraryImport(User32LibraryName, EntryPoint = "DefWindowProcW")]
    internal static partial nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport(User32LibraryName, EntryPoint = "SetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(nint hWnd);

    [LibraryImport(User32LibraryName, EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport(User32LibraryName, EntryPoint = "GetPhysicalCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetPhysicalCursorPos(out PhysicalCursorPoint lpPoint);

    [LibraryImport(User32LibraryName, EntryPoint = "CreatePopupMenu")]
    internal static partial nint CreatePopupMenu();

    [LibraryImport(User32LibraryName, EntryPoint = "AppendMenuW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AppendMenuW(nint hMenu, uint uFlags, nuint uIDNewItem, string? lpNewItem);

    [LibraryImport(User32LibraryName, EntryPoint = "TrackPopupMenuEx")]
    internal static partial int TrackPopupMenuEx(nint hMenu, uint uFlags, int x, int y, nint hwnd, nint lptpm);

    [LibraryImport(User32LibraryName, EntryPoint = "DestroyMenu")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyMenu(nint hMenu);

    [LibraryImport(Shell32LibraryName, EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW lpData);
}

internal delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

internal delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

[return: MarshalAs(UnmanagedType.Bool)]
internal delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, nint lprcMonitor, nint dwData);

[StructLayout(LayoutKind.Sequential)]
internal struct PhysicalCursorPoint
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MONITORINFO
{
    public int cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct APPBARDATA
{
    public uint cbSize;
    public nint hWnd;
    public uint uCallbackMessage;
    public uint uEdge;
    public RECT rc;
    public nint lParam;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct SHFILEINFOW
{
    public nint hIcon;
    public int iIcon;
    public uint dwAttributes;
    public fixed char szDisplayName[260];
    public fixed char szTypeName[80];
}

[StructLayout(LayoutKind.Sequential)]
internal struct WNDCLASSEXW
{
    public uint cbSize;
    public uint style;
    public nint lpfnWndProc;
    public int cbClsExtra;
    public int cbWndExtra;
    public nint hInstance;
    public nint hIcon;
    public nint hCursor;
    public nint hbrBackground;
    public nint lpszMenuName;
    public nint lpszClassName;
    public nint hIconSm;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct MONITORINFOEXW
{
    public int cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;
    public fixed char szDevice[32];
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct NOTIFYICONDATAW
{
    public uint cbSize;
    public nint hWnd;
    public uint uID;
    public uint uFlags;
    public uint uCallbackMessage;
    public nint hIcon;
    public fixed char szTip[128];
    public uint dwState;
    public uint dwStateMask;
    public fixed char szInfo[256];
    public uint uTimeoutOrVersion;
    public fixed char szInfoTitle[64];
    public uint dwInfoFlags;
    public Guid guidItem;
    public nint hBalloonIcon;
}
