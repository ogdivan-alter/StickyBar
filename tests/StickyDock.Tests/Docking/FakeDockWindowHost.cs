using StickyDock.Core.Docking;

namespace StickyDock.Tests.Docking;

/// <summary>
/// Recording double of <see cref="IDockWindowHost"/>: it keeps the window wherever the coordinator puts it and records
/// every applied rectangle, so the acceptance criteria of E2 are asserted without opening a window (ADR-002).
/// </summary>
internal sealed class FakeDockWindowHost : IDockWindowHost
{
    private readonly List<PixelRect> _appliedBounds = [];

    /// <summary>Gets or sets the size WPF would lay out for the bar.</summary>
    public DockBarSize BarSize { get; set; } = new(120d, 36d);

    /// <summary>Gets or sets the rectangle the window currently occupies.</summary>
    public PixelRect Bounds { get; set; } = new(700, 400, 150, 45);

    /// <summary>Gets the rectangles requested through <see cref="ApplyBounds"/>, in order.</summary>
    public IReadOnlyList<PixelRect> AppliedBounds => _appliedBounds;

    /// <summary>Gets the number of drag requests received.</summary>
    public int BeginDragCount { get; private set; }

    /// <inheritdoc />
    public DockBarSize GetBarSize() => BarSize;

    /// <inheritdoc />
    public PixelRect GetBounds() => Bounds;

    /// <inheritdoc />
    public void ApplyBounds(PixelRect bounds)
    {
        _appliedBounds.Add(bounds);
        Bounds = bounds;
    }

    /// <inheritdoc />
    public void BeginDrag() => BeginDragCount++;
}
