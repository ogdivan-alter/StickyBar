using StickyDock.Core.Configuration;
using StickyDock.Core.Docking;
using StickyDock.Core.Interaction;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Configuration;

/// <summary>
/// E3-CA5 and ADR-003, D6: the grace period of the collapse timer is persisted in <c>settings.json</c> with schema
/// version 2, a document written by E0–E2 (version 1) is migrated without losing its docked edge, and a stored value
/// out of range is normalized when it is read. The value is the one Q-03 fixed: 400 ms by default, 300–500 ms allowed.
/// </summary>
public sealed class GracePeriodPersistenceTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public void TheDefaultDocument_IsSchemaTwoWithTheDefaultGracePeriod()
    {
        var configuration = new StickyDockConfiguration();

        Assert.Equal(2, StickyDockConfiguration.CurrentSchemaVersion);
        Assert.Equal(2, configuration.SchemaVersion);
        Assert.Equal(GracePeriod.DefaultMilliseconds, configuration.GracePeriodMilliseconds);
        Assert.Equal(400, configuration.GracePeriodMilliseconds);
    }

    [Fact]
    public async Task SaveAsync_WritesTheSchemaVersionTwo()
    {
        var store = CreateStore();

        await store.SaveAsync(new StickyDockConfiguration());

        var content = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains("\"schemaVersion\": 2", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_WritesTheGracePeriodWithItsShortName()
    {
        var store = CreateStore();

        await store.SaveAsync(new StickyDockConfiguration());

        var content = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains($"\"{StickyDockConfiguration.GracePeriodPropertyName}\": 400", content, StringComparison.Ordinal);
        Assert.Contains("\"gracePeriodMs\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_FromASchemaOneDocument_AddsTheGracePeriodAndKeepsTheDockedEdge()
    {
        // M-08: this is the file an installation of E2 leaves behind.
        var store = CreateStore();
        await File.WriteAllTextAsync(store.FilePath, "{ \"schemaVersion\": 1, \"dockEdge\": 2 }");

        var loaded = await store.LoadAsync();

        Assert.Equal(2, loaded.SchemaVersion);
        Assert.Equal(GracePeriod.DefaultMilliseconds, loaded.GracePeriodMilliseconds);
        Assert.Equal(DockEdge.Right, loaded.DockEdge);
    }

    [Fact]
    public async Task LoadAsync_FromASchemaOneDocument_KeepsAGracePeriodItAlreadyHas()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(store.FilePath, "{ \"schemaVersion\": 1, \"gracePeriodMs\": 350 }");

        var loaded = await store.LoadAsync();

        Assert.Equal(2, loaded.SchemaVersion);
        Assert.Equal(350, loaded.GracePeriodMilliseconds);
    }

    [Theory]
    [InlineData(GracePeriod.MinimumMilliseconds)]
    [InlineData(GracePeriod.DefaultMilliseconds)]
    [InlineData(GracePeriod.MaximumMilliseconds)]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsTheGracePeriod(int milliseconds)
    {
        var store = CreateStore();

        await store.SaveAsync(new StickyDockConfiguration { GracePeriodMilliseconds = milliseconds });
        var loaded = await store.LoadAsync();

        Assert.Equal(milliseconds, loaded.GracePeriodMilliseconds);
    }

    [Fact]
    public async Task LoadAsync_FromARestartedStore_KeepsTheGracePeriod()
    {
        // E3-CA5: the chosen value survives closing and reopening the application.
        var store = CreateStore();
        await store.SaveAsync(new StickyDockConfiguration { GracePeriodMilliseconds = 300 });

        var restartedStore = CreateStore();
        var loaded = await restartedStore.LoadAsync();

        Assert.Equal(300, loaded.GracePeriodMilliseconds);
    }

    [Theory]
    [InlineData(9000, GracePeriod.MaximumMilliseconds)]
    [InlineData(0, GracePeriod.MinimumMilliseconds)]
    [InlineData(-400, GracePeriod.MinimumMilliseconds)]
    public async Task LoadAsync_WithAValueOutOfRange_IsNormalizedWhenItIsApplied(int stored, int expected)
    {
        var store = CreateStore();
        await store.SaveAsync(new StickyDockConfiguration { GracePeriodMilliseconds = stored });

        var loaded = await store.LoadAsync();

        // The file keeps what the user wrote; the range is enforced where the timer is armed.
        Assert.Equal(stored, loaded.GracePeriodMilliseconds);
        Assert.Equal(expected, GracePeriod.Normalize(loaded.GracePeriodMilliseconds));
    }

    [Fact]
    public async Task LoadAsync_FromADocumentWithoutTheProperty_UsesTheDefault()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(store.FilePath, "{ \"schemaVersion\": 2, \"dockEdge\": 1 }");

        var loaded = await store.LoadAsync();

        Assert.Equal(GracePeriod.DefaultMilliseconds, loaded.GracePeriodMilliseconds);
        Assert.Equal(DockEdge.Left, loaded.DockEdge);
    }

    private IConfigurationStore<StickyDockConfiguration> CreateStore()
        => StickyDockSettings.CreateStore(_directory.GetFilePath("settings.json"));
}
