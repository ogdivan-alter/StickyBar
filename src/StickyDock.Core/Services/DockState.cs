namespace StickyDock.Core.Services;

/// <summary>
/// State of the dock bar (ADR-003, D4).
/// </summary>
/// <remarks>
/// The state is never persisted: the bar always appears in <see cref="Tab"/>, which is the idle state described by §4.2.
/// The grace period is not a state of its own; it runs inside <see cref="Expanded"/> with a pending timer, so
/// <see cref="Collapsing"/> always means that the slide-out is running.
/// </remarks>
public enum DockState
{
    /// <summary>The bar is collapsed to its idle tab. No timer is armed.</summary>
    Tab = 0,

    /// <summary>The bar is deployed. Its footprint is the expanded one.</summary>
    Expanded = 1,

    /// <summary>The pointer is over the tab and the activation timer is pending. The footprint is still the collapsed one.</summary>
    Arming = 2,

    /// <summary>The slide-out is running. The footprint is still the expanded one until the settle timer fires.</summary>
    Collapsing = 3,
}
