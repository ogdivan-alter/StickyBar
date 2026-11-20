using StickyBar.Core.Configuration;

namespace StickyBar.Tests.TestInfrastructure;

/// <summary>
/// Atomic writer that fails on a chosen call, used to prove that a failed write leaves the previous document intact.
/// </summary>
internal sealed class FailingAtomicFileWriter : IAtomicFileWriter
{
    private readonly int _failingCallNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="FailingAtomicFileWriter"/> class.
    /// </summary>
    /// <param name="failingCallNumber">One-based number of the call that must throw.</param>
    public FailingAtomicFileWriter(int failingCallNumber) => _failingCallNumber = failingCallNumber;

    /// <summary>
    /// Gets the number of write attempts received.
    /// </summary>
    public int CallCount { get; private set; }

    /// <inheritdoc />
    public async Task WriteAsync(string filePath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        CallCount++;

        if (CallCount >= _failingCallNumber)
        {
            throw new IOException("Simulated interruption while writing the configuration file.");
        }

        await File.WriteAllBytesAsync(filePath, content.ToArray(), cancellationToken).ConfigureAwait(false);
    }
}
