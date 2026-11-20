using StickyBar.Core.Configuration;

namespace StickyBar.Tests.Docking;

/// <summary>
/// In-memory configuration store that records every saved document, so the persistence of the docked edge is asserted
/// without touching the file system. Saves complete synchronously, so the assertion is immediate.
/// </summary>
internal sealed class RecordingConfigurationStore : IConfigurationStore<StickyBarConfiguration>
{
    private readonly List<StickyBarConfiguration> _saved = [];

    /// <summary>Gets the document returned by <see cref="LoadAsync"/>.</summary>
    public StickyBarConfiguration Initial { get; } = new();

    /// <summary>Gets the documents written through <see cref="SaveAsync"/>, in order.</summary>
    public IReadOnlyList<StickyBarConfiguration> Saved => _saved;

    /// <summary>Gets the last document written, or <see langword="null"/> when nothing was saved.</summary>
    public StickyBarConfiguration? LastSaved => _saved.Count == 0 ? null : _saved[^1];

    /// <inheritdoc />
    public string FilePath => @"memory:\settings.json";

    /// <inheritdoc />
    public bool Exists => _saved.Count > 0;

    /// <inheritdoc />
    public Task<StickyBarConfiguration> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Initial);

    /// <inheritdoc />
    public Task SaveAsync(StickyBarConfiguration document, CancellationToken cancellationToken = default)
    {
        _saved.Add(document);
        return Task.CompletedTask;
    }
}
