using StickyDock.Core.Configuration;
using StickyDock.Core.Localization;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Configuration;

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
        var config = new StickyDockConfiguration();

        Assert.Equal(6, StickyDockConfiguration.CurrentSchemaVersion);
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
        await store.SaveAsync(new StickyDockConfiguration { Language = SupportedLanguages.English });

        var loaded = await store.LoadAsync();

        Assert.Equal(SupportedLanguages.English, loaded.Language);
    }

    private IConfigurationStore<StickyDockConfiguration> CreateStore()
        => StickyDockSettings.CreateStore(_directory.GetFilePath("settings.json"), new RecordingLogger());
}
