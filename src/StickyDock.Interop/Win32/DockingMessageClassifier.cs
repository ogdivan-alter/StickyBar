using StickyDock.Core.Docking;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Classifies the window messages the docking cares about. Pure and unit-testable, so the decision does not have to be
/// verified inside an <c>HwndSource</c> (ADR-002).
/// </summary>
public static class DockingMessageClassifier
{
    /// <summary><c>WM_SETTINGCHANGE</c>.</summary>
    public const int WindowSettingChange = 0x001A;

    /// <summary><c>WM_DISPLAYCHANGE</c>.</summary>
    public const int DisplayChange = 0x007E;

    /// <summary><c>WM_DPICHANGED</c>.</summary>
    public const int DpiChanged = 0x02E0;

    /// <summary><c>WM_EXITSIZEMOVE</c>.</summary>
    public const int ExitSizeMove = 0x0232;

    /// <summary><c>SPI_SETWORKAREA</c>, the <c>wParam</c> that identifies a work area change.</summary>
    public const int SetWorkArea = 0x002F;

    /// <summary>
    /// Decides whether a window message is one of the environment changes of the docking.
    /// </summary>
    /// <param name="message">Window message identifier.</param>
    /// <param name="wParam">First message parameter.</param>
    /// <param name="kind">Kind of change detected, or the default value when the message is not relevant.</param>
    /// <returns><see langword="true"/> when the message must be forwarded to the coordinator.</returns>
    /// <remarks>
    /// Only <c>WM_SETTINGCHANGE</c> needs its <c>wParam</c> inspected: Windows sends it for many system settings and
    /// only <c>SPI_SETWORKAREA</c> means that the taskbar moved or resized.
    /// </remarks>
    public static bool TryClassify(int message, nint wParam, out DockingEventKind kind)
    {
        switch (message)
        {
            case WindowSettingChange when wParam == SetWorkArea:
                kind = DockingEventKind.WorkAreaChanged;
                return true;

            case DisplayChange:
            case DpiChanged:
                kind = DockingEventKind.DisplayChanged;
                return true;

            case ExitSizeMove:
                kind = DockingEventKind.WindowDragCompleted;
                return true;

            default:
                kind = default;
                return false;
        }
    }
}
