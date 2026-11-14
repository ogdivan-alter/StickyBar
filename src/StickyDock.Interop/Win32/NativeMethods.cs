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
