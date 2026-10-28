using DockEdgeKind = StickyDock.Core.Docking.DockEdge;

namespace StickyDock.Core.Configuration;

/// <summary>
/// Root document persisted in <c>%AppData%\StickyDock\settings.json</c>.
/// </summary>
/// <remarks>
/// E0 defines the versioned envelope and the atomic read/write contract; the sections that hold the grace timer (E3),
/// the ribbons and shortcuts (E4, E5) and the monitor selection (E7) are added by later stages as new schema versions
/// with their own <see cref="IConfigurationMigration"/>.
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
    /// Gets the persisted dock edge (E2, AMP-01).
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> means the bar floats and keeps the position the user left it in. The default is
    /// <see cref="DockEdgeKind.Top"/>, which is where the first run already placed the bar (ADR-002). The property name
    /// and the numeric values of the enum are part of the persisted contract: <c>settings.json</c> stores
    /// <c>"dockEdge": 0..3</c> with the order <c>Top, Left, Right, Bottom</c>.
    /// </remarks>
    public DockEdgeKind? DockEdge { get; init; } = DockEdgeKind.Top;
}
