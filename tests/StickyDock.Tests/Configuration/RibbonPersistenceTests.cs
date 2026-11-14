using StickyDock.Core.Configuration;
using StickyDock.Core.Docking;
using StickyDock.Core.Interaction;
using StickyDock.Core.Ribbons;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Configuration;

/// <summary>
/// E4-CA4, §5.2-02, Q-10: ribbons are persisted in <c>settings.json</c> under schema version 3, documents from
/// earlier versions migrate cleanly without losing existing preferences, and open state is never persisted.
/// </summary>
public sealed class RibbonPersistenceTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public void TheDefaultDocument_HasEmptyRibbons()
    {
        var configuration = new StickyDockConfiguration();

        Assert.Equal(StickyDockConfiguration.CurrentSchemaVersion, configuration.SchemaVersion);
        Assert.Empty(configuration.Ribbons);
    }

    [Fact]
    public async Task SaveAsync_WritesCurrentSchemaVersionAndRibbons()
    {
        var store = CreateStore();
        var config = new StickyDockConfiguration
        {
            Ribbons =
            [
                new Ribbon { Id = "r1", Name = "Dev", Order = 0, Items = [] },
            ],
        };

        await store.SaveAsync(config);

        var content = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains($"\"schemaVersion\": {StickyDockConfiguration.CurrentSchemaVersion}", content, StringComparison.Ordinal);
        Assert.Contains("\"ribbons\"", content, StringComparison.Ordinal);
        Assert.Contains("\"Dev\"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("\"isOpen\"", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_FromSchemaTwoDocument_MigratesToCurrentWithEmptyRibbons()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(
            store.FilePath,
            "{ \"schemaVersion\": 2, \"dockEdge\": 1, \"gracePeriodMs\": 450 }");

        var loaded = await store.LoadAsync();

        Assert.Equal(StickyDockConfiguration.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.Equal(DockEdge.Left, loaded.DockEdge);
        Assert.Equal(450, loaded.GracePeriodMilliseconds);
        Assert.Empty(loaded.Ribbons);
    }

    [Fact]
    public async Task LoadAsync_FromSchemaOneDocument_MigratesToCurrent()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(store.FilePath, "{ \"schemaVersion\": 1, \"dockEdge\": 3 }");

        var loaded = await store.LoadAsync();

        Assert.Equal(StickyDockConfiguration.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.Equal(DockEdge.Bottom, loaded.DockEdge);
        Assert.Equal(GracePeriod.DefaultMilliseconds, loaded.GracePeriodMilliseconds);
        Assert.Empty(loaded.Ribbons);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsRibbonsAndItems()
    {
        var store = CreateStore();
        var config = new StickyDockConfiguration
        {
            Ribbons =
            [
                new Ribbon
                {
                    Id = "r1",
                    Name = "Tools",
                    Order = 0,
                    Items = [new RibbonItem("i1", "Terminal", @"C:\Apps\wt.lnk", 0)],
                },
                new Ribbon
                {
                    Id = "r2",
                    Name = "Media",
                    Order = 1,
                    Items = [],
                },
            ],
        };

        await store.SaveAsync(config);
        var loaded = await store.LoadAsync();

        Assert.Equal(2, loaded.Ribbons.Count);
        Assert.Equal("Tools", loaded.Ribbons[0].Name);
        Assert.Equal(1, loaded.Ribbons[0].ItemCount);
        Assert.Equal("Terminal", loaded.Ribbons[0].Items[0].Name);
        Assert.Equal("Media", loaded.Ribbons[1].Name);
    }

    private IConfigurationStore<StickyDockConfiguration> CreateStore()
        => StickyDockSettings.CreateStore(_directory.GetFilePath("settings.json"));
}
