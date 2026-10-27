using StickyDock.Core.Configuration;
using StickyDock.Core.Diagnostics;

namespace StickyDock.Core.Services;

/// <summary>
/// Edge docking state and logic (E2). Logic lives in Core, not in code-behind.
/// </summary>
public sealed class DockEdgeService
{
    /// <summary>Default snap threshold in pixels.</summary>
    public const int DefaultSnapThreshold = 48;

    private readonly ILogger _logger;
    private DockEdge? _currentEdge;

    /// <summary>
    /// Creates the service.
    /// </summary>
    public DockEdgeService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Current dock edge.</summary>
    public DockEdge? CurrentEdge => _currentEdge;

    /// <summary>Raised when edge changes.</summary>
    public event EventHandler<DockEdgeChangedEventArgs>? EdgeChanged;

    /// <summary>
    /// Sets the current edge.
    /// </summary>
    public void SetEdge(DockEdge? edge)
    {
        if (_currentEdge == edge)
        {
            return;
        }

        _currentEdge = edge;
        _logger.Debug($"Dock edge changed to {edge?.ToString() ?? "None"}.");
        EdgeChanged?.Invoke(this, new DockEdgeChangedEventArgs(edge));
    }

    /// <summary>
    /// Determines if the given edge is valid given taskbar edge (exclude taskbar edge).
    /// Spec: can dock to any of the 3 edges where taskbar is not located.
    /// </summary>
    public static bool IsValidEdge(DockEdge edge, TaskbarEdge taskbarEdge)
    {
        // Taskbar occupies one edge; we cannot dock to that edge
        return taskbarEdge switch
        {
            TaskbarEdge.Bottom => edge != DockEdge.Bottom,
            TaskbarEdge.Top => edge != DockEdge.Top,
            TaskbarEdge.Left => edge != DockEdge.Left,
            TaskbarEdge.Right => edge != DockEdge.Right,
            _ => true
        };
    }

    /// <summary>
    /// Computes snapped edge when releasing drag near edges, within threshold. Excludes taskbar edge.
    /// </summary>
    public static DockEdge? ComputeSnappedEdge(double mouseX, double mouseY, double screenWidth, double screenHeight, TaskbarEdge taskbarEdge, int threshold = DefaultSnapThreshold)
    {
        // Check proximity to edges (screen space) - prefer closest? but spec says snap to edge if within threshold
        // Top
        if (mouseY <= threshold)
        {
            var candidate = DockEdge.Top;
            if (IsValidEdge(candidate, taskbarEdge)) return candidate;
        }
        // Left
        if (mouseX <= threshold)
        {
            var candidate = DockEdge.Left;
            if (IsValidEdge(candidate, taskbarEdge)) return candidate;
        }
        // Right
        if (mouseX >= screenWidth - threshold)
        {
            var candidate = DockEdge.Right;
            if (IsValidEdge(candidate, taskbarEdge)) return candidate;
        }
        // Bottom
        if (mouseY >= screenHeight - threshold)
        {
            var candidate = DockEdge.Bottom;
            if (IsValidEdge(candidate, taskbarEdge)) return candidate;
        }

        return null;
    }
}

/// <summary>
/// Arguments for dock edge changes.
/// </summary>
public sealed record DockEdgeChangedEventArgs(DockEdge? Edge);
