using StickyDock.Core.Configuration;
using StickyDock.Core.Docking;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Configuration;

/// <summary>
/// E2-CA6: the docked edge is persisted in <c>settings.json</c> and survives a restart, with the numeric values that
/// already existed (<c>Top = 0 … Bottom = 3</c>) so no schema migration is needed (ADR-002, AMP-01).
/// </summary>
public sealed class DockEdgePersistenceTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public void TheDefaultConfiguration_DocksAtTheTop()
    {
        var configuration = new StickyDockConfiguration();

        Assert.Equal(DockEdge.Top, configuration.DockEdge);
    }

    [Theory]
    [InlineData(DockEdge.Top, 0)]
    [InlineData(DockEdge.Left, 1)]
    [InlineData(DockEdge.Right, 2)]
    [InlineData(DockEdge.Bottom, 3)]
    public async Task SaveAsync_WritesTheEdgeAsANumberWithThePersistedOrder(DockEdge edge, int expectedValue)
    {
        var store = CreateStore();

        await store.SaveAsync(new StickyDockConfiguration { DockEdge = edge });

        var content = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains($"\"dockEdge\": {expectedValue}", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_WithAFloatingBar_WritesNull()
    {
        var store = CreateStore();

        await store.SaveAsync(new StickyDockConfiguration { DockEdge = null });

        var content = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains("\"dockEdge\": null", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DockEdge.Top)]
    [InlineData(DockEdge.Left)]
    [InlineData(DockEdge.Right)]
    [InlineData(DockEdge.Bottom)]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsTheEdge(DockEdge edge)
    {
        var store = CreateStore();

        await store.SaveAsync(new StickyDockConfiguration { DockEdge = edge });
        var loaded = await store.LoadAsync();

        Assert.Equal(edge, loaded.DockEdge);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsTheFloatingState()
    {
        var store = CreateStore();

        await store.SaveAsync(new StickyDockConfiguration { DockEdge = null });
        var loaded = await store.LoadAsync();

        Assert.Null(loaded.DockEdge);
    }

    [Fact]
    public async Task LoadAsync_FromARestartedStore_ReturnsTheDockedEdge()
    {
        // Simulates the restart of the application: a new store instance reads the edge written by the previous run.
        var store = CreateStore();
        await store.SaveAsync(new StickyDockConfiguration { DockEdge = DockEdge.Right });

        var restartedStore = CreateStore();
        var loaded = await restartedStore.LoadAsync();

        Assert.Equal(DockEdge.Right, loaded.DockEdge);
    }

    [Fact]
    public async Task LoadAsync_WhenTheDocumentHasNoDockEdge_FallsBackToTheTop()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(store.FilePath, "{ \"schemaVersion\": 1 }");

        var loaded = await store.LoadAsync();

        Assert.Equal(DockEdge.Top, loaded.DockEdge);
    }

    private JsonConfigurationStore<StickyDockConfiguration> CreateStore()
        => new(
            _directory.GetFilePath("settings.json"),
            static () => new StickyDockConfiguration());
}
