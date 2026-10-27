namespace StickyDock.Core.Configuration;

/// <summary>
/// Root document persisted in <c>%AppData%\StickyDock\settings.json</c>.
/// </summary>
/// <remarks>
/// E0 defines the versioned envelope and the atomic read/write contract; the sections that hold the docked edge (E2),
/// the grace timer (E3), the ribbons and shortcuts (E4, E5) and the monitor selection (E7) are added by later stages
/// as new schema versions with their own <see cref="IConfigurationMigration"/>.
/// </remarks>
public sealed record StickyDockConfiguration : IVersionedDocument
{
    /// <summary>
    /// Schema version written and understood by this build.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <inheritdoc />
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Persisted dock edge preference (E2). Null means not docked / auto-position.
    /// </summary>
    public DockEdge? DockEdge { get; init; }
}

/// <summary>
/// Edge where the dock bar is attached. Plan states Top/Left/Right; include Bottom for flexibility.
/// </summary>
public enum DockEdge
{
    /// <summary>Docked to the top edge of the work area.</summary>
    Top,

    /// <summary>Docked to the left edge of the work area.</summary>
    Left,

    /// <summary>Docked to the right edge of the work area.</summary>
    Right,

    /// <summary>Docked to the bottom edge of the work area.</summary>
    Bottom
}
