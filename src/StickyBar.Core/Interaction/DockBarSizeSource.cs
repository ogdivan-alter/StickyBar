using StickyBar.Core.Docking;
using StickyBar.Core.Services;

namespace StickyBar.Core.Interaction;

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
    private readonly IRibbonLayoutState? _ribbonLayout;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockBarSizeSource"/> class.
    /// </summary>
    /// <param name="states">State of the bar.</param>
    /// <param name="metrics">Declared footprints.</param>
    public DockBarSizeSource(IDockStateService states, DockBarMetrics metrics)
        : this(states, metrics, ribbonLayout: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockBarSizeSource"/> class with dynamic ribbon sizing (E4-CA6).
    /// </summary>
    /// <param name="states">State of the bar.</param>
    /// <param name="metrics">Declared footprints.</param>
    /// <param name="ribbonLayout">Optional source of ribbon/item counts and open-ribbon state.</param>
    public DockBarSizeSource(IDockStateService states, DockBarMetrics metrics, IRibbonLayoutState? ribbonLayout)
    {
        _states = states ?? throw new ArgumentNullException(nameof(states));
        _metrics = metrics;
        _ribbonLayout = ribbonLayout;
    }

    /// <inheritdoc />
    public DockBarSize GetCurrentSize(DockEdge? anchor)
    {
        // The deployed footprint stays while the bar slides out, so the window only shrinks once the slide has settled.
        var expanded = _states.State is DockState.Expanded or DockState.Collapsing;

        if (_ribbonLayout is null)
        {
            return _metrics.GetSize(expanded, anchor);
        }

        return _metrics.GetDynamicSize(
            expanded,
            anchor,
            _ribbonLayout.RibbonCount,
            _ribbonLayout.BarItemCount,
            _ribbonLayout.HasOpenRibbon,
            _ribbonLayout.OpenRibbonItemCount,
            _ribbonLayout.IsOpenRibbonActionsOpen);
    }
}
