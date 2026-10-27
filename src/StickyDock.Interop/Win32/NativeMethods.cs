using System.Runtime.InteropServices;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Raw P/Invoke declarations. They are internal on purpose: the rest of the product uses the safe wrappers of this
/// assembly and never handles native types (AGENTS.md).
/// </summary>
internal static partial class NativeMethods
{
    private const string User32LibraryName = "user32.dll";
    private const string UxThemeLibraryName = "uxtheme.dll";
    private const string DwmApiLibraryName = "dwmapi.dll";
    private const string Shell32LibraryName = "shell32.dll";

    [LibraryImport(User32LibraryName, EntryPoint = "GetSystemMetrics")]
    internal static partial int GetSystemMetrics(int index);

    [LibraryImport(User32LibraryName, EntryPoint = "GetForegroundWindow")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport(User32LibraryName, EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

    [LibraryImport(User32LibraryName, EntryPoint = "GetClientRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetClientRect(nint hWnd, out RECT lpRect);

    [LibraryImport(User32LibraryName, EntryPoint = "MonitorFromWindow")]
    internal static partial nint MonitorFromWindow(nint hWnd, uint dwFlags);

    [LibraryImport(User32LibraryName, EntryPoint = "GetMonitorInfoW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfoW(nint hMonitor, ref MONITORINFO lpmi);

    [LibraryImport(User32LibraryName, EntryPoint = "SystemParametersInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SystemParametersInfoW(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

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

    [LibraryImport(User32LibraryName, EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out POINT lpPoint);

    [LibraryImport(User32LibraryName, EntryPoint = "ScreenToClient")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ScreenToClient(nint hWnd, ref POINT lpPoint);

    [LibraryImport(User32LibraryName, EntryPoint = "ClientToScreen")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ClientToScreen(nint hWnd, ref POINT lpPoint);

    [LibraryImport(Shell32LibraryName, EntryPoint = "SHAppBarMessage")]
    internal static partial nint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    [LibraryImport("advapi32.dll", EntryPoint = "RegOpenKeyExW")]
    internal static partial int RegOpenKeyExW(nint hKey, [MarshalAs(UnmanagedType.LPWStr)] string subKey, int ulOptions, int samDesired, out nint phkResult);

    [LibraryImport("advapi32.dll", EntryPoint = "RegQueryValueExW")]
    internal static partial int RegQueryValueExW(nint hKey, [MarshalAs(UnmanagedType.LPWStr)] string lpValueName, nint lpReserved, out uint lpType, System.Span<byte> lpData, ref uint lpcbData);

    [LibraryImport("advapi32.dll", EntryPoint = "RegCloseKey")]
    internal static partial int RegCloseKey(nint hKey);

    [DllImport(DwmApiLibraryName, EntryPoint = "DwmEnableBlurBehindWindow")]
    internal static extern int DwmEnableBlurBehindWindow(nint hWnd, ref DWM_BLURBEHIND pBlurBehind);
}

internal delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

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
internal struct DWM_BLURBEHIND
{
    public uint dwFlags;
    [MarshalAs(UnmanagedType.Bool)]
    public bool fEnable;
    public nint hRgnBlur;
    [MarshalAs(UnmanagedType.Bool)]
    public bool fTransitionOnMaximized;
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
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
