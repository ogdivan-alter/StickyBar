namespace StickyDock.Core.Ribbons;

/// <summary>
/// Flat shortcut entry contained inside a <see cref="Ribbon"/> or on the main bar (E4, E5).
/// </summary>
/// <param name="Id">Unique identifier of the item.</param>
/// <param name="Name">Display name of the shortcut.</param>
/// <param name="ShortcutPath">Path to the <c>.lnk</c> file.</param>
/// <param name="Order">Zero-based position within its container.</param>
public sealed record RibbonItem(
    string Id,
    string Name,
    string ShortcutPath,
    int Order);
