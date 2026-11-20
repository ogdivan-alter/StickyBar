using StickyBar.Core.Docking;

namespace StickyBar.Interop.Win32;

/// <summary>
/// <c>EVENT_OBJECT_LOCATIONCHANGE</c> filtered to the taskbar window of the current session.
/// </summary>
/// <remarks>
/// <para>
/// Windows moves and hides the taskbar without sending <c>SPI_SETWORKAREA</c>, so this hook complements the window
/// messages. Three filters keep the cost at zero in repose (H-E2-04, RNF-02): the hook is installed for the process of
/// <c>explorer.exe</c>, the callback discards any window that is not <c>Shell_TrayWnd</c>, and
/// <see cref="Changed"/> is only raised when the taskbar placement actually changed.
/// </para>
/// <para>
/// Callbacks arrive on the thread that installs the hook (<c>WINEVENT_OUTOFCONTEXT</c>): install it on the UI thread.
/// If <c>explorer.exe</c> restarts, the hook goes inert until the next start; the message route keeps working and is
/// registered as debt in ADR-002.
/// </para>
/// </remarks>
public sealed class WinEventDockingEventSource : IDockingEventSource
{
    private const uint WindowEventOutOfContext = 0x0000;
    private const uint EventObjectLocationChange = 0x800B;
    private const int ObjectIdWindow = 0;
    private const string TaskbarWindowClass = "Shell_TrayWnd";

    private readonly WinEventDelegate _callback;
    private readonly nint _taskbarWindow;

    private nint _hook;
    private TaskbarPlacement? _lastPlacement;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WinEventDockingEventSource"/> class.
    /// </summary>
    public WinEventDockingEventSource()
    {
        _callback = OnWinEvent;
        _taskbarWindow = NativeMethods.FindWindowW(TaskbarWindowClass, null);

        if (_taskbarWindow == nint.Zero)
        {
            // Without the taskbar window the hook cannot be filtered by window, and an unfiltered hook fires with any
            // window of any process: exactly the cost H-E2-04 reported. The message route remains in charge.
            return;
        }

        var threadId = NativeMethods.GetWindowThreadProcessId(_taskbarWindow, out var processId);

        if (threadId == 0 || processId == 0)
        {
            return;
        }

        _lastPlacement = TaskbarMonitor.GetTaskbarPlacement(_taskbarWindow);

        _hook = NativeMethods.SetWinEventHook(
            EventObjectLocationChange,
            EventObjectLocationChange,
            nint.Zero,
            _callback,
            processId,
            0,
            WindowEventOutOfContext);
    }

    /// <inheritdoc />
    public event EventHandler<DockingEventArgs>? Changed;

    /// <summary>
    /// Gets a value indicating whether at least one native hook handle is installed.
    /// </summary>
    /// <remarks>Diagnostic member, used to verify that disposal releases the native resources (E2-CA5).</remarks>
    public bool IsActive => _hook != nint.Zero;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_hook != nint.Zero)
        {
            _ = NativeMethods.UnhookWinEvent(_hook);
            _hook = nint.Zero;
        }

        _lastPlacement = null;
        Changed = null;
    }

    private void OnWinEvent(
        nint hWinEventHook,
        uint eventType,
        nint hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (_disposed || hwnd != _taskbarWindow || idObject != ObjectIdWindow)
        {
            return;
        }

        var placement = TaskbarMonitor.GetTaskbarPlacement(_taskbarWindow);

        if (_lastPlacement == placement)
        {
            return;
        }

        _lastPlacement = placement;
        Changed?.Invoke(this, new DockingEventArgs(DockingEventKind.TaskbarChanged));
    }
}
