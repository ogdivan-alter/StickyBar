namespace StickyDock.Core.Docking;

/// <summary>
/// Pure geometry of the docking: no state, no native calls and no UI framework, so every rule of E2 is decided here and
/// verified by unit tests (ADR-002).
/// </summary>
public static class DockPlacementCalculator
{
    /// <summary>
    /// Width of the snap band, in device-independent units (A3).
    /// </summary>
    /// <remarks>
    /// This is the single adjustment point of the snap: <see cref="ComputeSnapTarget"/> converts it to device pixels
    /// with the scale of the monitor, so the magnet feels the same at 100 % and at 150 %.
    /// </remarks>
    public const double SnapThresholdDips = 48d;

    /// <summary>
    /// Edges of the screen in declaration order. Ties of the snap are broken with this order.
    /// </summary>
    private static readonly DockEdge[] AllEdges = [DockEdge.Top, DockEdge.Left, DockEdge.Right, DockEdge.Bottom];

    /// <summary>
    /// Determines whether <paramref name="edge"/> can host the bar.
    /// </summary>
    /// <param name="edge">Edge to test.</param>
    /// <param name="taskbar">Taskbar of the monitor that hosts the bar.</param>
    /// <returns>
    /// <see langword="true"/> unless the taskbar occupies that edge on the current monitor (E2-CA4). When the probe
    /// could not locate the taskbar, no edge is excluded.
    /// </returns>
    public static bool IsEdgeAvailable(DockEdge edge, TaskbarPlacement taskbar)
        => !(taskbar.IsOnCurrentMonitor && taskbar.Edge == edge);

    /// <summary>
    /// Chooses the edge the bar must keep or move to.
    /// </summary>
    /// <param name="current">Edge the bar is docked to, or <see langword="null"/> while it floats.</param>
    /// <param name="taskbar">Taskbar of the monitor that hosts the bar.</param>
    /// <returns>
    /// <paramref name="current"/> when it is still available; otherwise the opposite edge; otherwise the first
    /// available edge in the order <c>Top</c>, <c>Left</c>, <c>Right</c>, <c>Bottom</c>.
    /// </returns>
    /// <remarks>Never fails: at most one edge hosts the taskbar, so at least three are always available.</remarks>
    public static DockEdge ChooseDockEdge(DockEdge? current, TaskbarPlacement taskbar)
    {
        if (current is { } edge && IsEdgeAvailable(edge, taskbar))
        {
            return edge;
        }

        if (current is { } occupied)
        {
            var opposite = GetOpposite(occupied);

            if (IsEdgeAvailable(opposite, taskbar))
            {
                return opposite;
            }
        }

        foreach (var candidate in AllEdges)
        {
            if (IsEdgeAvailable(candidate, taskbar))
            {
                return candidate;
            }
        }

        // Unreachable: the taskbar occupies one edge at most, so a candidate is always available.
        return DockEdge.Top;
    }

    /// <summary>
    /// Gets the rectangle the bar must occupy when docked to <paramref name="edge"/>.
    /// </summary>
    /// <param name="edge">Edge the bar is docked to.</param>
    /// <param name="monitor">Monitor that hosts the bar.</param>
    /// <param name="size">Size of the bar as laid out by the UI framework.</param>
    /// <returns>
    /// A rectangle inside the work area, centred along the edge. When the bar does not fit, it is clipped so that it
    /// never leaves the work area (E2-CA3).
    /// </returns>
    public static PixelRect GetDockedBounds(DockEdge edge, MonitorGeometry monitor, DockBarSize size)
    {
        var workArea = monitor.WorkArea;
        var scale = monitor.Scale;

        var width = Math.Min(
            (int)Math.Round(size.WidthDips * scale, MidpointRounding.AwayFromZero),
            workArea.Width);
        var height = Math.Min(
            (int)Math.Round(size.HeightDips * scale, MidpointRounding.AwayFromZero),
            workArea.Height);

        var centredX = workArea.X + ((workArea.Width - width) / 2);
        var centredY = workArea.Y + ((workArea.Height - height) / 2);

        return edge switch
        {
            DockEdge.Top => new PixelRect(centredX, workArea.Y, width, height),
            DockEdge.Bottom => new PixelRect(centredX, workArea.Bottom - height, width, height),
            DockEdge.Left => new PixelRect(workArea.X, centredY, width, height),
            DockEdge.Right => new PixelRect(workArea.Right - width, centredY, width, height),

            // Unreachable: DockEdge has four members. Kept explicit so that a new member fails visibly.
            _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, "Unknown dock edge."),
        };
    }

    /// <summary>
    /// Decides the edge the bar snaps to when the user releases it.
    /// </summary>
    /// <param name="draggedBounds">Rectangle of the bar as the user left it, in device pixels.</param>
    /// <param name="monitor">Monitor that hosts the bar.</param>
    /// <param name="taskbar">Taskbar of the monitor that hosts the bar.</param>
    /// <param name="thresholdDips">Width of the snap band, in device-independent units.</param>
    /// <returns>
    /// The nearest available edge when the distance between the bar and that edge is within the band; otherwise
    /// <see langword="null"/>, which means the bar must stay floating where the user left it (E2-CA1).
    /// </returns>
    /// <remarks>
    /// The distance is measured between the rectangle of the bar and the edge of the work area, not from the pointer,
    /// so it does not depend on where the window was grabbed. A bar released past the edge counts as distance zero.
    /// Edges occupied by the taskbar are discarded before measuring, so they can never win (E2-CA4).
    /// </remarks>
    public static DockEdge? ComputeSnapTarget(
        PixelRect draggedBounds,
        MonitorGeometry monitor,
        TaskbarPlacement taskbar,
        double thresholdDips = SnapThresholdDips)
    {
        var workArea = monitor.WorkArea;
        var thresholdPixels = (int)Math.Round(thresholdDips * monitor.Scale, MidpointRounding.AwayFromZero);

        DockEdge? target = null;
        var shortestDistance = int.MaxValue;

        foreach (var edge in AllEdges)
        {
            if (!IsEdgeAvailable(edge, taskbar))
            {
                continue;
            }

            var distance = edge switch
            {
                DockEdge.Top => Math.Max(0, draggedBounds.Y - workArea.Y),
                DockEdge.Left => Math.Max(0, draggedBounds.X - workArea.X),
                DockEdge.Right => Math.Max(0, workArea.Right - draggedBounds.Right),
                DockEdge.Bottom => Math.Max(0, workArea.Bottom - draggedBounds.Bottom),
                _ => int.MaxValue,
            };

            // Strict comparison keeps the first edge of the declaration order when two distances tie.
            if (distance <= thresholdPixels && distance < shortestDistance)
            {
                shortestDistance = distance;
                target = edge;
            }
        }

        return target;
    }

    /// <summary>
    /// Brings a floating rectangle back inside the work area.
    /// </summary>
    /// <param name="bounds">Rectangle of the floating bar, in device pixels.</param>
    /// <param name="workArea">Work area of the monitor.</param>
    /// <returns>
    /// The same rectangle when it already fits; otherwise one fully contained in the work area. The size is only
    /// reduced when the bar is larger than the work area, which is what keeps it on screen after a resolution change
    /// (E2-CA3).
    /// </returns>
    public static PixelRect ClampIntoWorkArea(PixelRect bounds, PixelRect workArea)
    {
        if (bounds.IsEmpty || workArea.IsEmpty)
        {
            return bounds;
        }

        var width = Math.Min(bounds.Width, workArea.Width);
        var height = Math.Min(bounds.Height, workArea.Height);

        var x = Math.Clamp(bounds.X, workArea.X, workArea.Right - width);
        var y = Math.Clamp(bounds.Y, workArea.Y, workArea.Bottom - height);

        return new PixelRect(x, y, width, height);
    }

    private static DockEdge GetOpposite(DockEdge edge) => edge switch
    {
        DockEdge.Top => DockEdge.Bottom,
        DockEdge.Bottom => DockEdge.Top,
        DockEdge.Left => DockEdge.Right,
        DockEdge.Right => DockEdge.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, "Unknown dock edge."),
    };
}
