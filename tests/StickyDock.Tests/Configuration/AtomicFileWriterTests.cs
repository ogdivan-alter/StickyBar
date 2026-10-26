using System.Text;
using StickyDock.Core.Configuration;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Configuration;

/// <summary>
/// E0-CA4: an interrupted write never leaves the destination half written.
/// </summary>
public sealed class AtomicFileWriterTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly AtomicFileWriter _writer = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public async Task WriteAsync_WhenTheDestinationDoesNotExist_CreatesIt()
    {
        var destinationPath = _directory.GetFilePath("settings.json");
        var content = Encode("{ \"schemaVersion\": 1 }");

        await _writer.WriteAsync(destinationPath, content);

        Assert.Equal(content, await File.ReadAllBytesAsync(destinationPath));
        Assert.Empty(_directory.GetFiles("*.tmp"));
    }

    [Fact]
    public async Task WriteAsync_WhenTheDestinationExists_ReplacesTheContentAndLeavesNoTemporaryFile()
    {
        var destinationPath = _directory.GetFilePath("settings.json");
        await _writer.WriteAsync(destinationPath, Encode("first"));

        await _writer.WriteAsync(destinationPath, Encode("second"));

        Assert.Equal("second", await File.ReadAllTextAsync(destinationPath));
        Assert.Empty(_directory.GetFiles("*.tmp"));
    }

    [Fact]
    public async Task WriteAsync_WhenTheDirectoryDoesNotExist_CreatesIt()
    {
        var destinationPath = Path.Combine(_directory.Path, "nested", "deeper", "settings.json");

        await _writer.WriteAsync(destinationPath, Encode("content"));

        Assert.True(File.Exists(destinationPath));
    }

    [Fact]
    public async Task WriteAsync_WhenCancelled_LeavesThePreviousContentAndNoTemporaryFile()
    {
        var destinationPath = _directory.GetFilePath("settings.json");
        var previousContent = Encode("previous");
        await _writer.WriteAsync(destinationPath, previousContent);

        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _writer.WriteAsync(destinationPath, Encode("new content"), cancellationSource.Token));

        Assert.Equal(previousContent, await File.ReadAllBytesAsync(destinationPath));
        Assert.Empty(_directory.GetFiles("*.tmp"));
    }

    [Fact]
    public async Task WriteAsync_WhenTheDestinationIsLocked_LeavesThePreviousContentAndNoTemporaryFile()
    {
        var destinationPath = _directory.GetFilePath("settings.json");
        var previousContent = Encode("previous");
        await _writer.WriteAsync(destinationPath, previousContent);

        await using (new FileStream(destinationPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            await Assert.ThrowsAnyAsync<IOException>(() => _writer.WriteAsync(destinationPath, Encode("new content")));
        }

        Assert.Equal(previousContent, await File.ReadAllBytesAsync(destinationPath));
        Assert.Empty(_directory.GetFiles("*.tmp"));
    }

    [Fact]
    public async Task WriteAsync_WhenThePathIsEmpty_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _writer.WriteAsync(" ", Encode("content")));
    }

    private static byte[] Encode(string content) => Encoding.UTF8.GetBytes(content);
}
