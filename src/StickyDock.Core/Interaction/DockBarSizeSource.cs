using StickyDock.Core.Docking;
using StickyDock.Core.Services;

namespace StickyDock.Core.Interaction;

/// <summary>
/// Default size source: the dock state decides which of the two declared footprints applies.
/// </summary>
/// <remarks>
/// It lives in Core and not in the application on purpose: the size that the coordinator applies is then verifiable with
/// a double of <see cref="IDockStateService"/>, and the application contributes no decision (ADR-003, D2).
/// </remarks>
public sealed class DockBarSizeSource : IDockBarSizeSource
{
    private readonly IDockStateService _states;
    private readonly DockBarMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockBarSizeSource"/> class.
    /// </summary>
    /// <param name="states">State of the bar.</param>
    /// <param name="metrics">Declared footprints.</param>
    public DockBarSizeSource(IDockStateService states, DockBarMetrics metrics)
    {
        _states = states ?? throw new ArgumentNullException(nameof(states));
        _metrics = metrics;
    }

    /// <inheritdoc />
    public DockBarSize GetCurrentSize(DockEdge? anchor)
    {
        // The deployed footprint stays while the bar slides out, so the window only shrinks once the slide has settled.
        var expanded = _states.State is DockState.Expanded or DockState.Collapsing;

        return _metrics.GetSize(expanded, anchor);
    }
}
