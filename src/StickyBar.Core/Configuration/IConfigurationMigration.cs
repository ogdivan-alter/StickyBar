using System.Text.Json.Nodes;

namespace StickyBar.Core.Configuration;

/// <summary>
/// Transforms the raw JSON of a stored document from one schema version to the next.
/// </summary>
/// <remarks>
/// Migrations work on the JSON tree, before deserialization, so a newer application can read a document written
/// by an older one without keeping legacy model types around.
/// </remarks>
public interface IConfigurationMigration
{
    /// <summary>
    /// Gets the schema version the migration reads; the migration produces <see cref="FromVersion"/> plus one.
    /// </summary>
    int FromVersion { get; }

    /// <summary>
    /// Applies the migration in place. Property names use the serialized (camelCase) convention.
    /// </summary>
    /// <param name="document">Raw document being upgraded.</param>
    void Apply(JsonObject document);
}
