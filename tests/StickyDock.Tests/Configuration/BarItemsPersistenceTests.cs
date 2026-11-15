using StickyDock.Core.Configuration;
using StickyDock.Core.Docking;
using StickyDock.Core.Ribbons;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Configuration;

/// <summary>
/// E5-CA5, E5-CA6, §5.2-02: main-bar shortcuts are persisted under schema version 4, v3 documents migrate cleanly to
/// v4 without losing ribbons, and ordering is preserved across reload.
/// </summary>
public sealed class BarItemsPersistenceTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public void TheDefaultDocument_IsSchemaFiveWithEmptyBarItems()
    {
        var configuration = new StickyDockConfiguration();

        Assert.Equal(StickyDockConfiguration.CurrentSchemaVersion, configuration.SchemaVersion);
        Assert.Empty(configuration.BarItems);
    }

    [Fact]
    public async Task SaveAsync_WritesCurrentSchemaVersionAndBarItems()
    {
        var store = CreateStore();
        var config = new StickyDockConfiguration
        {
            BarItems =
            [
                new RibbonItem("b1", "Terminal", @"C:\Apps\wt.lnk", 0),
            ],
        };

        await store.SaveAsync(config);

        var content = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains($"\"schemaVersion\": {StickyDockConfiguration.CurrentSchemaVersion}", content, StringComparison.Ordinal);
        Assert.Contains("\"barItems\"", content, StringComparison.Ordinal);
        Assert.Contains("\"Terminal\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_FromSchemaThreeDocument_MigratesToCurrentPreservingRibbons()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(
            store.FilePath,
            """
            {
              "schemaVersion": 3,
              "dockEdge": 2,
              "gracePeriodMs": 380,
              "ribbons": [
                {
                  "id": "r1",
                  "name": "Dev",
                  "order": 0,
                  "items": [
                    { "id": "i1", "name": "WSL", "shortcutPath": "C:\\Apps\\wsl.lnk", "order": 0 }
                  ]
                }
              ]
            }
            """);

        var loaded = await store.LoadAsync();

        Assert.Equal(StickyDockConfiguration.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.Equal(DockEdge.Right, loaded.DockEdge);
        Assert.Equal(380, loaded.GracePeriodMilliseconds);
        Assert.Empty(loaded.BarItems);
        var ribbon = Assert.Single(loaded.Ribbons);
        Assert.Equal("Dev", ribbon.Name);
        var item = Assert.Single(ribbon.Items);
        Assert.Equal("WSL", item.Name);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsBarItemsAndRibbonsInExactOrder()
    {
        var store = CreateStore();
        var config = new StickyDockConfiguration
        {
            BarItems =
            [
                new RibbonItem("b2", "Second", @"C:\Apps\second.lnk", 1),
                new RibbonItem("b1", "First", @"C:\Apps\first.lnk", 0),
            ],
            Ribbons =
            [
                new Ribbon
                {
                    Id = "r1",
                    Name = "Tools",
                    Order = 0,
                    Items =
                    [
                        new RibbonItem("i1", "Alpha", @"C:\Apps\alpha.lnk", 0),
                        new RibbonItem("i2", "Beta", @"C:\Apps\beta.lnk", 1),
                    ],
                },
            ],
        };

        await store.SaveAsync(config);
        var loaded = await store.LoadAsync();

        Assert.Equal(2, loaded.BarItems.Count);
        Assert.Equal("Second", loaded.BarItems[0].Name);
        Assert.Equal(1, loaded.BarItems[0].Order);
        Assert.Equal("First", loaded.BarItems[1].Name);
        Assert.Equal(0, loaded.BarItems[1].Order);
        Assert.Equal(2, loaded.Ribbons[0].Items.Count);
        Assert.Equal("Alpha", loaded.Ribbons[0].Items[0].Name);
        Assert.Equal("Beta", loaded.Ribbons[0].Items[1].Name);
    }

    private IConfigurationStore<StickyDockConfiguration> CreateStore()
        => StickyDockSettings.CreateStore(_directory.GetFilePath("settings.json"));
}
