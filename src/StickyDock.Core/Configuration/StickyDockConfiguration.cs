using System.Text.Json.Serialization;
using StickyDock.Core.Interaction;
using StickyDock.Core.Ribbons;
using DockEdgeKind = StickyDock.Core.Docking.DockEdge;

namespace StickyDock.Core.Configuration;

/// <summary>
/// Root document persisted in <c>%AppData%\StickyDock\settings.json</c>.
/// </summary>
/// <remarks>
/// E0 defines the versioned envelope and the atomic read/write contract; E2 adds the docked edge, E3 the grace period of
/// the collapse timer (schema version 2), E4 the flat ribbons (schema version 3), and later stages the shortcuts and the
/// monitor selection, each as a new schema version with its own <see cref="IConfigurationMigration"/>.
/// </remarks>
public sealed record StickyDockConfiguration : IVersionedDocument
{
    /// <summary>
    /// Serialized name of <see cref="GracePeriodMilliseconds"/>.
    /// </summary>
    /// <remarks>
    /// The naming policy of the store is camelCase, which would produce <c>gracePeriodMilliseconds</c>; the short name is
    /// the one declared by ADR-003 and the one the migration writes.
    /// </remarks>
    public const string GracePeriodPropertyName = "gracePeriodMs";

    /// <summary>
    /// Serialized name of <see cref="Ribbons"/>.
    /// </summary>
    public const string RibbonsPropertyName = "ribbons";

    /// <summary>
    /// Serialized name of <see cref="BarItems"/>.
    /// </summary>
    public const string BarItemsPropertyName = "barItems";

    /// <summary>
    /// Schema version written and understood by this build.
    /// </summary>
    public const int CurrentSchemaVersion = 4;

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

    /// <summary>
    /// Gets the grace period of the collapse timer, in milliseconds (E3, Q-03).
    /// </summary>
    /// <remarks>
    /// Values outside <see cref="GracePeriod.MinimumMilliseconds"/>..<see cref="GracePeriod.MaximumMilliseconds"/> are
    /// normalized on read by <see cref="GracePeriod.Normalize"/>; the persisted value is whatever the user chose. The
    /// default is <see cref="GracePeriod.DefaultMilliseconds"/>.
    /// </remarks>
    [JsonPropertyName(GracePeriodPropertyName)]
    public int GracePeriodMilliseconds { get; init; } = GracePeriod.DefaultMilliseconds;

    /// <summary>
    /// Gets the persisted ribbons in display order (E4, §5.2-02). Open/closed state is transient and never persisted
    /// (Q-10).
    /// </summary>
    [JsonPropertyName(RibbonsPropertyName)]
    public IReadOnlyList<Ribbon> Ribbons { get; init; } = [];

    /// <summary>
    /// Gets the persisted shortcut items on the main dock bar in display order (E5, §5.2-02).
    /// </summary>
    [JsonPropertyName(BarItemsPropertyName)]
    public IReadOnlyList<RibbonItem> BarItems { get; init; } = [];
}
