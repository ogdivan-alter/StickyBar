using System.Windows.Interop;
using StickyBar.Core.Docking;
using StickyBar.Interop.Win32;

namespace StickyBar.App.Docking;

/// <summary>
/// Forwards the window messages of an <see cref="HwndSource"/> that the docking cares about.
/// </summary>
/// <remarks>
/// The classification itself lives in <see cref="DockingMessageClassifier"/>, which is pure and unit-tested; this class
/// only installs and removes the hook, so it contains no decision (ADR-002).
/// </remarks>
internal sealed class WpfMessageDockingEventSource : IDockingEventSource
{
    private readonly HwndSource _source;
    private readonly HwndSourceHook _hook;

    private bool _sawExitSizeMove;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfMessageDockingEventSource"/> class.
    /// </summary>
    /// <param name="source">Source of the window messages of the bar.</param>
    public WpfMessageDockingEventSource(HwndSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _hook = OnMessage;
        _source.AddHook(_hook);
    }

    /// <inheritdoc />
    public event EventHandler<DockingEventArgs>? Changed;

    /// <summary>
    /// Resets the <c>WM_EXITSIZEMOVE</c> observation flag before entering the native move loop.
    /// </summary>
    public void BeginNativeMoveTracking()
    {
        _sawExitSizeMove = false;
    }

    /// <summary>
    /// Emits <see cref="DockingEventKind.WindowDragCompleted"/> when the native move loop returned without
    /// <c>WM_EXITSIZEMOVE</c> (for example, a click without movement).
    /// </summary>
    public void EnsureDragCompletedAfterNativeMove()
    {
        if (_disposed || _sawExitSizeMove)
        {
            return;
        }

        _sawExitSizeMove = true;
        Changed?.Invoke(this, new DockingEventArgs(DockingEventKind.WindowDragCompleted));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _source.RemoveHook(_hook);
        Changed = null;
    }

    private nint OnMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (_disposed || !DockingMessageClassifier.TryClassify(message, wParam, out var kind))
        {
            return nint.Zero;
        }

        if (kind == DockingEventKind.WindowDragCompleted)
        {
            _sawExitSizeMove = true;
        }

        Changed?.Invoke(this, new DockingEventArgs(kind));

        return nint.Zero;
    }
}
