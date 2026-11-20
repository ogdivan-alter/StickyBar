namespace StickyBar.Core.Configuration;

/// <summary>
/// Default <see cref="IAtomicFileWriter"/>: writes a temporary file next to the destination, flushes it to disk and
/// then replaces the destination in a single file system operation.
/// </summary>
/// <remarks>
/// <para>
/// The temporary file lives in the destination directory on purpose: <see cref="File.Replace(string, string, string?)"/>
/// is only atomic while both files are on the same volume. A write that is interrupted before the replacement leaves
/// the previous configuration intact; the temporary file is removed on the way out.
/// </para>
/// <para>
/// The content is flushed to disk before the replacement so that a power loss cannot leave the destination pointing
/// at a file whose data has not reached the storage device yet.
/// </para>
/// </remarks>
public sealed class AtomicFileWriter : IAtomicFileWriter
{
    private const int BufferSizeInBytes = 4096;
    private const string TemporaryFileExtension = ".tmp";

    /// <inheritdoc />
    public async Task WriteAsync(string filePath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A destination file path is required.", nameof(filePath));
        }

        var destinationPath = Path.GetFullPath(filePath);
        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException($"The path '{destinationPath}' has no directory.");

        Directory.CreateDirectory(destinationDirectory);

        var temporaryPath = Path.Combine(
            destinationDirectory,
            $"{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}{TemporaryFileExtension}");

        try
        {
            await WriteTemporaryFileAsync(temporaryPath, content, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceDestination(temporaryPath, destinationPath);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static async Task WriteTemporaryFileAsync(
        string temporaryPath,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        var stream = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            BufferSizeInBytes,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using (stream.ConfigureAwait(false))
        {
            await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            FlushToDisk(stream);
        }
    }

    // Kept out of the async method on purpose: there is no asynchronous flush-to-disk, and durability before the
    // replacement is what keeps the destination readable after a crash.
    private static void FlushToDisk(FileStream stream) => stream.Flush(flushToDisk: true);

    private static void ReplaceDestination(string temporaryPath, string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            File.Replace(temporaryPath, destinationPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            return;
        }

        File.Move(temporaryPath, destinationPath);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // The temporary file is disposable: failing to remove it must not fail the write.
        }
        catch (UnauthorizedAccessException)
        {
            // Same as above: the destination already holds the valid content.
        }
    }
}
