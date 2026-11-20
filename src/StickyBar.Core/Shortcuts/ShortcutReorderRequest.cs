namespace StickyBar.Core.Shortcuts;

/// <summary>
/// Parameter passed by the UI reorder behavior to move a shortcut item to the slot of another item (E5-CA5).
/// </summary>
/// <param name="SourceItemId">Identifier of the dragged shortcut item.</param>
/// <param name="TargetItemId">Identifier of the drop-target shortcut item.</param>
public sealed record ShortcutReorderRequest(string SourceItemId, string TargetItemId);
