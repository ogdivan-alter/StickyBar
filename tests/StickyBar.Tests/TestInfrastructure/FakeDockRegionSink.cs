using StickyBar.Core.Interaction;

namespace StickyBar.Tests.TestInfrastructure;

/// <summary>
/// Recording double of <see cref="IDockRegionSink"/>: it stores the entries and exits the view model forwards, so the
/// forwarding commands are asserted without the interaction policy (ADR-003, D7).
/// </summary>
internal sealed class FakeDockRegionSink : IDockRegionSink
{
    private readonly List<DockRegion> _entered = [];
    private readonly List<DockRegion> _left = [];

    /// <summary>Gets the regions reported as entered, in order.</summary>
    public IReadOnlyList<DockRegion> Entered => _entered;

    /// <summary>Gets the regions reported as left, in order.</summary>
    public IReadOnlyList<DockRegion> Left => _left;

    /// <inheritdoc />
    public void EnterRegion(DockRegion region) => _entered.Add(region);

    /// <inheritdoc />
    public void LeaveRegion(DockRegion region) => _left.Add(region);
}
