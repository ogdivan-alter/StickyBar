using System.Runtime.InteropServices;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Encapsulates system hooks for docking/taskbar changes. Must be disposed to release handles/subscriptions (E2-CA5).
/// </summary>
public sealed class DockingHookService : IDisposable
{
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint EVENT_SYSTEM_MOVESIZEEND = 0x000B;
    private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

    private nint _winEventHook1 = nint.Zero;
    private nint _winEventHook2 = nint.Zero;
    private readonly WinEventDelegate _callback;
    private bool _disposed;

    /// <summary>
    /// Raised when taskbar/work area might have changed.
    /// </summary>
    public event EventHandler? WorkAreaChanged;

    /// <summary>
    /// Creates the hook service.
    /// </summary>
    public DockingHookService()
    {
        _callback = OnWinEvent;
        // Subscribe to location/move events; also rely on WM_SETTINGCHANGE in window proc
        _winEventHook1 = NativeMethods.SetWinEventHook(EVENT_SYSTEM_MOVESIZEEND, EVENT_SYSTEM_MOVESIZEEND, nint.Zero, _callback, 0, 0, WINEVENT_OUTOFCONTEXT);
        _winEventHook2 = NativeMethods.SetWinEventHook(EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_LOCATIONCHANGE, nint.Zero, _callback, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    private void OnWinEvent(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        try
        {
            // Best effort notification
            WorkAreaChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Do not throw from hook callback
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_winEventHook1 != nint.Zero)
        {
            NativeMethods.UnhookWinEvent(_winEventHook1);
            _winEventHook1 = nint.Zero;
        }

        if (_winEventHook2 != nint.Zero)
        {
            NativeMethods.UnhookWinEvent(_winEventHook2);
            _winEventHook2 = nint.Zero;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
