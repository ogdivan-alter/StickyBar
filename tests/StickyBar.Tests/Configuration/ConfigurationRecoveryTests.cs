using StickyBar.Core.Configuration;
using StickyBar.Core.Docking;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.Configuration;

/// <summary>
/// E8-CA3: verifies that a corrupt, truncated, or incompatible configuration file never prevents startup and
/// recovers with default values while logging a warning.
/// </summary>
public sealed class ConfigurationRecoveryTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Theory]
    [InlineData("{ not valid json")]
    [InlineData("{ \"schemaVersion\": ")]
    [InlineData("[]")]
    [InlineData("{ \"schemaVersion\": 999 }")]
    public async Task LoadOrDefaultAsync_WhenConfigurationIsCorruptOrInvalid_ReturnsDefaultsAndLogsWarning(string content)
    {
        var filePath = _directory.GetFilePath("settings.json");
        await File.WriteAllTextAsync(filePath, content);
        var logger = new RecordingLogger();
        var store = StickyBarSettings.CreateStore(filePath, logger);

        var configuration = await StickyBarSettings.LoadOrDefaultAsync(store, logger);

        Assert.NotNull(configuration);
        Assert.Equal(StickyBarConfiguration.CurrentSchemaVersion, configuration.SchemaVersion);
        Assert.Equal(DockEdge.Top, configuration.DockEdge);
        Assert.Contains(logger.Entries, entry => entry.StartsWith("Warning:", StringComparison.Ordinal));
    }
}
