using StickyDock.Core.Configuration;

namespace StickyDock.Tests.Docking;

/// <summary>
/// In-memory configuration store that records every saved document, so the persistence of the docked edge is asserted
/// without touching the file system. Saves complete synchronously, so the assertion is immediate.
/// </summary>
internal sealed class RecordingConfigurationStore : IConfigurationStore<StickyDockConfiguration>
{
    private readonly List<StickyDockConfiguration> _saved = [];

    /// <summary>Gets the document returned by <see cref="LoadAsync"/>.</summary>
    public StickyDockConfiguration Initial { get; } = new();

    /// <summary>Gets the documents written through <see cref="SaveAsync"/>, in order.</summary>
    public IReadOnlyList<StickyDockConfiguration> Saved => _saved;

    /// <summary>Gets the last document written, or <see langword="null"/> when nothing was saved.</summary>
    public StickyDockConfiguration? LastSaved => _saved.Count == 0 ? null : _saved[^1];

    /// <inheritdoc />
    public string FilePath => @"memory:\settings.json";

    /// <inheritdoc />
    public bool Exists => _saved.Count > 0;

    /// <inheritdoc />
    public Task<StickyDockConfiguration> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Initial);

    /// <inheritdoc />
    public Task SaveAsync(StickyDockConfiguration document, CancellationToken cancellationToken = default)
    {
        _saved.Add(document);
        return Task.CompletedTask;
    }
}
