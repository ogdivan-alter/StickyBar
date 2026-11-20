using StickyBar.Core.Services;

namespace StickyBar.Core.Interaction;

/// <summary>
/// Declares which regions can exist in which state (ADR-004, D3). It is the single translation of the
/// <c>Visibility</c> the view declares for each element: a region may only be active while its element paints.
/// </summary>
/// <remarks>
/// <para>
/// This is what makes a stuck region unrepresentable. The pointer events of WPF are not guaranteed to be symmetric —an
/// element that is hidden, collapsed or clipped never raises its own exit—, so the machine cannot trust an exit to make
/// the interaction area shrink back. Instead the state is the authority: an impossible region is dropped on entry and
/// purged when the state changes.
/// </para>
/// <para>
/// Pure and stateless, so the table is verified without a window.
/// </para>
/// </remarks>
public static class DockRegionPresence
{
    /// <summary>
    /// Gets a value indicating whether <paramref name="region"/> belongs to the content that is only painted while the
    /// bar is deployed. Content regions are purged when the bar leaves the deployed footprint.
    /// </summary>
    /// <param name="region">Region to classify.</param>
    /// <returns>
    /// <see langword="true"/> for <see cref="DockRegion.Bar"/> and <see cref="DockRegion.Ribbon"/>; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public static bool IsContent(DockRegion region) => region is DockRegion.Bar or DockRegion.Ribbon;

    /// <summary>
    /// Gets a value indicating whether <paramref name="region"/> can be active while the bar is in
    /// <paramref name="state"/>.
    /// </summary>
    /// <param name="region">Region the pointer would be in.</param>
    /// <param name="state">State of the bar at that moment.</param>
    /// <returns>
    /// <see langword="true"/> for the tab in the four states; for the bar and the ribbons only in
    /// <see cref="DockState.Expanded"/> and <see cref="DockState.Collapsing"/>; never for
    /// <see cref="DockRegion.None"/>.
    /// </returns>
    public static bool IsPresent(DockRegion region, DockState state)
        => region switch
        {
            DockRegion.Tab => true,
            DockRegion.Bar or DockRegion.Ribbon
                => state is DockState.Expanded or DockState.Collapsing,
            _ => false,
        };
}
