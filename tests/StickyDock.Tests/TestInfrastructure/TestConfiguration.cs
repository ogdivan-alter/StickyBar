using StickyDock.Core.Configuration;

namespace StickyDock.Tests.TestInfrastructure;

/// <summary>
/// Document used to exercise the persistence mechanism with real payloads, independently of the production schema.
/// </summary>
internal sealed record TestConfiguration : IVersionedDocument
{
    /// <summary>Schema version written by this test document.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <inheritdoc />
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets the name used to check round-trip fidelity.</summary>
    public string DisplayName { get; init; } = "default";

    /// <summary>Gets the collection used to check that lists survive the round trip.</summary>
    public IReadOnlyList<string> Items { get; init; } = [];
}
