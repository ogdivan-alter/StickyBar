using StickyBar.Core.Docking;

namespace StickyBar.Tests.Docking;

/// <summary>
/// Double of <see cref="IDockBarSizeSource"/>: it declares the footprint of the bar for a state instead of measuring it,
/// so the tests of the coordinator can change the size as the dock state machine would (ADR-003, D2).
/// </summary>
internal sealed class FakeBarSizeSource : IDockBarSizeSource
{
    /// <summary>Gets or sets the declared footprints. Defaults to the design values of the product.</summary>
    public DockBarMetrics Metrics { get; set; } = DockBarMetrics.Default;

    /// <summary>Gets or sets a value indicating whether the deployed footprint applies.</summary>
    public bool Expanded { get; set; }

    /// <inheritdoc />
    public DockBarSize GetCurrentSize(DockEdge? anchor) => Metrics.GetSize(Expanded, anchor);
}
