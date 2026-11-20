namespace StickyBar.Tests.TestInfrastructure;

/// <summary>
/// Isolated directory for tests that touch the file system. It is removed on dispose.
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemporaryDirectory"/> class with a unique path.
    /// </summary>
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "stickybar-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Gets the full path of the directory.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Builds a full path inside the directory.
    /// </summary>
    /// <param name="fileName">Name of the file or folder.</param>
    /// <returns>The combined path.</returns>
    public string GetFilePath(string fileName) => System.IO.Path.Combine(Path, fileName);

    /// <summary>
    /// Gets the files of the directory that match a pattern.
    /// </summary>
    /// <param name="searchPattern">Search pattern, for example <c>*.tmp</c>.</param>
    /// <returns>The matching file paths.</returns>
    public string[] GetFiles(string searchPattern) => Directory.GetFiles(Path, searchPattern);

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // A locked file must not fail the test run; the temporary folder is disposable by definition.
        }
        catch (UnauthorizedAccessException)
        {
            // Same as above.
        }
    }
}
