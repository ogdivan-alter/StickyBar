namespace StickyBar.Core.Configuration;

/// <summary>
/// Reads and writes the versioned configuration document.
/// </summary>
/// <typeparam name="TDocument">Document type persisted by the store.</typeparam>
public interface IConfigurationStore<TDocument>
    where TDocument : class, IVersionedDocument
{
    /// <summary>
    /// Gets the full path of the configuration file.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Gets a value indicating whether the configuration file already exists.
    /// </summary>
    bool Exists { get; }

    /// <summary>
    /// Loads the configuration, migrating older documents and returning defaults when no file exists yet.
    /// </summary>
    /// <param name="cancellationToken">Token that cancels the read.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="ConfigurationLoadException">The file exists but cannot be read or parsed.</exception>
    /// <exception cref="ConfigurationSchemaException">The schema version cannot be handled.</exception>
    Task<TDocument> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the configuration atomically, replacing any previous content.
    /// </summary>
    /// <param name="document">Document to persist.</param>
    /// <param name="cancellationToken">Token that cancels the write.</param>
    /// <returns>A task that completes when the new content is on disk.</returns>
    Task SaveAsync(TDocument document, CancellationToken cancellationToken = default);
}
