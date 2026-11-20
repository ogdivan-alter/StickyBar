using System.Text.Json.Serialization;

namespace StickyBar.Core.Ribbons;

/// <summary>
/// Flat container of shortcut entries displayed perpendicularly to the dock bar (RF03, HU03, E4-CA3).
/// </summary>
/// <remarks>
/// Nesting is forbidden by type: a <see cref="Ribbon"/> can only contain <see cref="RibbonItem"/> entries, never
/// another <see cref="Ribbon"/>.
/// </remarks>
public sealed record Ribbon
{
    /// <summary>Gets the unique identifier of the ribbon.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the display name of the ribbon.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the zero-based position of the ribbon along the dock bar.</summary>
    public int Order { get; init; }

    /// <summary>Gets the flat list of shortcut items inside this ribbon.</summary>
    public IReadOnlyList<RibbonItem> Items { get; init; } = [];

    /// <summary>Gets the number of shortcut items inside this ribbon.</summary>
    [JsonIgnore]
    public int ItemCount => Items.Count;
}
