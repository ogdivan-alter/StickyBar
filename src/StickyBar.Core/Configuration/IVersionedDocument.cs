namespace StickyBar.Core.Configuration;

/// <summary>
/// Document that declares the schema version it was written with.
/// </summary>
/// <remarks>
/// The version is part of the persisted content, not of the file name, so a document can be read, migrated and
/// written back without renaming files. See §5.2-02 and AMP-02 (JSON instead of SQLite).
/// </remarks>
public interface IVersionedDocument
{
    /// <summary>
    /// Gets the schema version of the document.
    /// </summary>
    int SchemaVersion { get; }
}
