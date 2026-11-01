namespace StickyDock.Core.Configuration;

/// <summary>
/// Arguments of <see cref="IConfigurationDocument.Changed"/>.
/// </summary>
/// <param name="Document">Document as it is after the change.</param>
public sealed record ConfigurationChangedEventArgs(StickyDockConfiguration Document);

/// <summary>
/// The one mutable configuration of the process (D-07).
/// </summary>
/// <remarks>
/// <para>
/// Every consumer holds the same instance and the only way to change it is to ask for a transformation, so no writer can
/// overwrite the preferences of another one. <see cref="IConfigurationStore{TDocument}"/> stays the pure input/output
/// port and its only caller is this document (ADR-003, D1).
/// </para>
/// <para>
/// The document keeps the immutable record shape: <see cref="Update"/> produces a new value and replaces the current
/// one. Use it from the UI thread; <see cref="FlushAsync"/> may be awaited from any thread.
/// </para>
/// </remarks>
public interface IConfigurationDocument
{
    /// <summary>
    /// Gets the current document. The instance is replaced on every change.
    /// </summary>
    StickyDockConfiguration Current { get; }

    /// <summary>
    /// Raised after a change, with the document that is current at that moment.
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? Changed;

    /// <summary>
    /// Replaces the document with the result of <paramref name="change"/> and schedules a write.
    /// </summary>
    /// <param name="change">Transformation applied to the current document.</param>
    /// <returns>
    /// <see langword="false"/> when the transformation produced an equal document, in which case there is no write and
    /// no event.
    /// </returns>
    bool Update(Func<StickyDockConfiguration, StickyDockConfiguration> change);

    /// <summary>
    /// Awaits the write in flight and writes the document again when it is still dirty.
    /// </summary>
    /// <param name="cancellationToken">Token that cancels the wait.</param>
    /// <returns>A task that completes when the document on disk is the current one.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
