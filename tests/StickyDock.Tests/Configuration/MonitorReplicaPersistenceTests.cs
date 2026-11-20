using StickyDock.Core.Configuration;
using StickyDock.Core.Docking;
using StickyDock.Core.Ribbons;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Configuration;

/// <summary>
/// E7, §4.6-02: enabled replica monitors are persisted under schema version 5, v4 documents migrate cleanly to v5
/// without losing ribbons or bar items, and device names round-trip accurately.
/// </summary>
public sealed class MonitorReplicaPersistenceTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public void TheDefaultDocument_IsCurrentSchemaWithEmptyEnabledReplicaMonitors()
    {
        var configuration = new StickyDockConfiguration();

        Assert.Equal(StickyDockConfiguration.CurrentSchemaVersion, configuration.SchemaVersion);
        Assert.Empty(configuration.EnabledReplicaMonitors);
    }

    [Fact]
    public async Task SaveAsync_WritesCurrentSchemaVersionAndEnabledReplicaMonitors()
    {
        var store = CreateStore();
        var config = new StickyDockConfiguration
        {
            EnabledReplicaMonitors = [@"\\.\DISPLAY2", @"\\.\DISPLAY3"],
        };

        await store.SaveAsync(config);

        var content = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains($"\"schemaVersion\": {StickyDockConfiguration.CurrentSchemaVersion}", content, StringComparison.Ordinal);
        Assert.Contains("\"enabledReplicaMonitors\"", content, StringComparison.Ordinal);
        Assert.Contains("DISPLAY2", content, StringComparison.Ordinal);
        Assert.Contains("DISPLAY3", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_FromSchemaFourDocument_MigratesToCurrentSchemaPreservingBarItemsAndRibbons()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(
            store.FilePath,
            """
            {
              "schemaVersion": 4,
              "dockEdge": 1,
              "gracePeriodMs": 420,
              "ribbons": [
                {
                  "id": "r1",
                  "name": "Tools",
                  "order": 0,
                  "items": []
                }
              ],
              "barItems": [
                { "id": "b1", "name": "Shell", "shortcutPath": "C:\\Apps\\pwsh.lnk", "order": 0 }
              ]
            }
            """);

        var loaded = await store.LoadAsync();

        Assert.Equal(StickyDockConfiguration.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.Equal(DockEdge.Left, loaded.DockEdge);
        Assert.Equal(420, loaded.GracePeriodMilliseconds);
        Assert.Empty(loaded.EnabledReplicaMonitors);
        Assert.Equal("Tools", Assert.Single(loaded.Ribbons).Name);
        Assert.Equal("Shell", Assert.Single(loaded.BarItems).Name);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsEnabledReplicaMonitors()
    {
        var store = CreateStore();
        var config = new StickyDockConfiguration
        {
            EnabledReplicaMonitors = [@"\\.\DISPLAY2"],
            BarItems = [new RibbonItem("b1", "Terminal", @"C:\Apps\wt.lnk", 0)],
        };

        await store.SaveAsync(config);
        var loaded = await store.LoadAsync();

        Assert.Equal([@"\\.\DISPLAY2"], loaded.EnabledReplicaMonitors);
        Assert.Equal("Terminal", Assert.Single(loaded.BarItems).Name);
    }

    private IConfigurationStore<StickyDockConfiguration> CreateStore()
        => StickyDockSettings.CreateStore(_directory.GetFilePath("settings.json"));
}
