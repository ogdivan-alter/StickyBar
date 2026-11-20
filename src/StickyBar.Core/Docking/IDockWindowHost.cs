namespace StickyBar.Core.Docking;

/// <summary>
/// The one window the docking coordinator drives.
/// </summary>
/// <remarks>
/// Implemented by <c>StickyBar.App</c> over WPF. Every member must be called on the thread that owns the window; the
/// implementation enforces it (dispatches when needed) and the coordinator makes no assumption about it (ADR-002).
/// </remarks>
public interface IDockWindowHost
{
    /// <summary>
    /// Gets the current window rectangle.
    /// </summary>
    /// <returns>The rectangle of the window, in device pixels.</returns>
    PixelRect GetBounds();

    /// <summary>
    /// Moves and resizes the window.
    /// </summary>
    /// <param name="bounds">Authoritative rectangle of the window, in device pixels.</param>
    void ApplyBounds(PixelRect bounds);

    /// <summary>
    /// Starts a drag of the window with the native move loop of the system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contingency of ADR-002: the window has no non-client caption (the bar is entirely client area so that the
    /// pointer events of E1 and E3 keep arriving), so the drag is delegated to the move loop of the system.
    /// </para>
    /// <para>
    /// The loop is started with <c>WM_SYSCOMMAND(SC_MOVE)</c>, which is an accepted implementation deviation from the
    /// <c>WM_NCLBUTTONDOWN / HTCAPTION</c> text of ADR-002: with <c>AllowsTransparency="true"</c> and a
    /// <c>CaptionHeight</c> of zero, the non-client button message does not enter the modal loop reliably, whereas the
    /// system menu command does and follows the cursor until the button is released.
    /// </para>
    /// <para>
    /// The end of the drag still surfaces as <c>WM_EXITSIZEMOVE</c>, which is what the coordinator observes.
    /// </para>
    /// </remarks>
    void BeginDrag();
}
