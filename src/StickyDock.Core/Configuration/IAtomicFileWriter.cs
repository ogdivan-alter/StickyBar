namespace StickyDock.Core.Configuration;

/// <summary>
/// Writes a file so that readers only ever observe the previous content or the complete new content.
/// </summary>
/// <remarks>
/// The abstraction exists so that a failed or interrupted write can be reproduced in tests without corrupting
/// the files of the developer profile (E0-CA4).
/// </remarks>
public interface IAtomicFileWriter
{
    /// <summary>
    /// Writes <paramref name="content"/> to <paramref name="filePath"/>, replacing the previous content atomically.
    /// </summary>
    /// <param name="filePath">Destination file.</param>
    /// <param name="content">Complete content of the new file.</param>
    /// <param name="cancellationToken">Token that cancels the write; cancelling leaves the destination untouched.</param>
    /// <returns>A task that completes when the destination holds the new content.</returns>
    Task WriteAsync(string filePath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
}
