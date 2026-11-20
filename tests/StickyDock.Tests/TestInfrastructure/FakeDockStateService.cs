using StickyDock.Core.Services;

namespace StickyDock.Tests.TestInfrastructure;

/// <summary>
/// Double of <see cref="IDockStateService"/>: keeps the state and raises the event when it changes, so the projections
/// that depend on the state are tested without the interaction policy (ADR-003, D7).
/// </summary>
internal sealed class FakeDockStateService : IDockStateService
{
    /// <inheritdoc />
    public DockState State { get; private set; } = DockState.Tab;

    /// <inheritdoc />
    public bool IsPinned { get; set; }

    /// <inheritdoc />
    public event EventHandler<DockStateChangedEventArgs>? StateChanged;

    /// <summary>Gets the number of times <see cref="Expand"/> was called.</summary>
    public int ExpandCount { get; private set; }

    /// <summary>Gets the number of times <see cref="CollapseToTab"/> was called.</summary>
    public int CollapseCount { get; private set; }

    /// <summary>Moves the double to a state and raises the event, as the real controller would.</summary>
    /// <param name="state">New state.</param>
    public void SetState(DockState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(this, new DockStateChangedEventArgs(state));
    }

    /// <inheritdoc />
    public void CollapseToTab()
    {
        CollapseCount++;
        SetState(DockState.Tab);
    }

    /// <inheritdoc />
    public void Expand()
    {
        ExpandCount++;
        SetState(DockState.Expanded);
    }

    /// <inheritdoc />
    public void BeginDrag() { }

    /// <inheritdoc />
    public void EndDrag() { }
}
