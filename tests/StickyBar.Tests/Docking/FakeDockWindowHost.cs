using StickyBar.Core.Docking;

namespace StickyBar.Tests.Docking;

/// <summary>
/// Recording double of <see cref="IDockWindowHost"/>: it keeps the window wherever the coordinator puts it and records
/// every applied rectangle, so the acceptance criteria of E2 are asserted without opening a window (ADR-002).
/// </summary>
internal sealed class FakeDockWindowHost : IDockWindowHost
{
    private readonly List<PixelRect> _appliedBounds = [];

    /// <summary>Gets or sets the rectangle the window currently occupies.</summary>
    public PixelRect Bounds { get; set; } = new(700, 400, 150, 45);

    /// <summary>Gets the rectangles requested through <see cref="ApplyBounds"/>, in order.</summary>
    public IReadOnlyList<PixelRect> AppliedBounds => _appliedBounds;

    /// <summary>Gets the number of drag requests received.</summary>
    public int BeginDragCount { get; private set; }

    /// <summary>
    /// Gets or sets an action invoked inside <see cref="ApplyBounds"/>, once the window is already at the new rectangle.
    /// It lets a test provoke the reentry that rule R2 of ADR-003 protects against.
    /// </summary>
    public Action<PixelRect>? OnApplyBounds { get; set; }

    /// <inheritdoc />
    public PixelRect GetBounds() => Bounds;

    /// <inheritdoc />
    public void ApplyBounds(PixelRect bounds)
    {
        _appliedBounds.Add(bounds);
        Bounds = bounds;
        OnApplyBounds?.Invoke(bounds);
    }

    /// <inheritdoc />
    public void BeginDrag() => BeginDragCount++;
}
