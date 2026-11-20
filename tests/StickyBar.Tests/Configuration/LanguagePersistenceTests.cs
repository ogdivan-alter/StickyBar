using StickyBar.Core.Configuration;
using StickyBar.Core.Localization;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.Configuration;

/// <summary>
/// Verifies schema version 6 language persistence (default Spanish <c>"es"</c>, configurable to <c>"en"</c>) and
/// v5 → v6 migration.
/// </summary>
public sealed class LanguagePersistenceTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public void DefaultConfiguration_IsSchemaSixWithSpanishLanguage()
    {
        var config = new StickyBarConfiguration();

        Assert.Equal(6, StickyBarConfiguration.CurrentSchemaVersion);
        Assert.Equal(6, config.SchemaVersion);
        Assert.Equal(SupportedLanguages.Spanish, config.Language);
    }

    [Fact]
    public async Task LoadAsync_FromSchemaFiveDocument_MigratesToSixWithSpanishDefault()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(
            store.FilePath,
            """
            {
              "schemaVersion": 5,
              "dockEdge": 0,
              "gracePeriodMs": 400,
              "ribbons": [],
              "barItems": [],
              "enabledReplicaMonitors": []
            }
            """);

        var loaded = await store.LoadAsync();

        Assert.Equal(6, loaded.SchemaVersion);
        Assert.Equal(SupportedLanguages.Spanish, loaded.Language);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsEnglishLanguage()
    {
        var store = CreateStore();
        await store.SaveAsync(new StickyBarConfiguration { Language = SupportedLanguages.English });

        var loaded = await store.LoadAsync();

        Assert.Equal(SupportedLanguages.English, loaded.Language);
    }

    private IConfigurationStore<StickyBarConfiguration> CreateStore()
        => StickyBarSettings.CreateStore(_directory.GetFilePath("settings.json"), new RecordingLogger());
}
