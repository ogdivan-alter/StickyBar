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
}
