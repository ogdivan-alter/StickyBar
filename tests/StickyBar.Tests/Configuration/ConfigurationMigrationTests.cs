using StickyBar.Core.Configuration;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.Configuration;

/// <summary>
/// Schema versioning: older documents are migrated before deserialization, newer ones are rejected.
/// </summary>
public sealed class ConfigurationMigrationTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public async Task LoadAsync_WhenTheDocumentIsOlder_MigratesItAndUpgradesTheVersion()
    {
        await WriteRawAsync("{ \"schemaVersion\": 0 }");
        var store = CreateStore(new AddDefaultDisplayNameMigration());

        var document = await store.LoadAsync();

        Assert.Equal(TestConfiguration.CurrentSchemaVersion, document.SchemaVersion);
        Assert.Equal("default", document.DisplayName);
    }

    [Fact]
    public async Task LoadAsync_WhenTheVersionIsMissing_TreatsTheDocumentAsVersionZero()
    {
        await WriteRawAsync("{}");
        var store = CreateStore(new AddDefaultDisplayNameMigration());

        var document = await store.LoadAsync();

        Assert.Equal(TestConfiguration.CurrentSchemaVersion, document.SchemaVersion);
        Assert.Equal("default", document.DisplayName);
    }

    [Fact]
    public async Task LoadAsync_WhenNoMigrationCoversTheVersion_ThrowsConfigurationSchemaException()
    {
        await WriteRawAsync("{ \"schemaVersion\": 0 }");
        var store = CreateStore();

        await Assert.ThrowsAsync<ConfigurationSchemaException>(() => store.LoadAsync());
    }

    [Fact]
    public async Task SaveAsync_AfterAMigration_PersistsTheUpgradedVersion()
    {
        await WriteRawAsync("{ \"schemaVersion\": 0 }");
        var store = CreateStore(new AddDefaultDisplayNameMigration());
        var migrated = await store.LoadAsync();

        await store.SaveAsync(migrated);

        var content = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains("\"schemaVersion\": 1", content, StringComparison.Ordinal);
    }

    private JsonConfigurationStore<TestConfiguration> CreateStore(params IConfigurationMigration[] migrations)
        => new(
            _directory.GetFilePath("settings.json"),
            static () => new TestConfiguration(),
            migrations);

    private Task WriteRawAsync(string json)
        => File.WriteAllTextAsync(_directory.GetFilePath("settings.json"), json);
}
