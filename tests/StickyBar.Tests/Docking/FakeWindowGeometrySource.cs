using StickyBar.Core.Docking;

namespace StickyBar.Tests.Docking;

/// <summary>
/// Double of <see cref="IWindowGeometrySource"/> whose monitor and taskbar are set by the test, so the same scenario can
/// be replayed with a different work area, resolution or taskbar edge.
/// </summary>
internal sealed class FakeWindowGeometrySource : IWindowGeometrySource
{
    /// <summary>Gets or sets the monitor returned by <see cref="GetMonitorGeometry"/>.</summary>
    public MonitorGeometry Monitor { get; set; } = new(new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040), 120);

    /// <summary>Gets or sets the taskbar returned by <see cref="GetTaskbarPlacement"/>.</summary>
    public TaskbarPlacement Taskbar { get; set; } =
        new(TaskbarState.Visible, DockEdge.Bottom, PixelRect.Empty, IsOnCurrentMonitor: true);

    /// <summary>Gets the number of times the monitor was read.</summary>
    public int MonitorReadCount { get; private set; }

    /// <summary>Gets the number of times the taskbar was read.</summary>
    public int TaskbarReadCount { get; private set; }

    /// <inheritdoc />
    public MonitorGeometry GetMonitorGeometry()
    {
        MonitorReadCount++;
        return Monitor;
    }

    /// <inheritdoc />
    public TaskbarPlacement GetTaskbarPlacement()
    {
        TaskbarReadCount++;
        return Taskbar;
    }
}
