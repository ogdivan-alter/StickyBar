using StickyBar.Core.Configuration;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.Configuration;

/// <summary>
/// E0-CA3 (the schema is created, read back and survives a restart) and E0-CA4 (writes are atomic) at the store level.
/// </summary>
public sealed class JsonConfigurationStoreTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    /// <inheritdoc />
    public void Dispose() => _directory.Dispose();

    [Fact]
    public async Task LoadAsync_WhenTheFileDoesNotExist_ReturnsTheDefaultDocument()
    {
        var store = CreateStore();

        var document = await store.LoadAsync();

        Assert.False(store.Exists);
        Assert.Equal(TestConfiguration.CurrentSchemaVersion, document.SchemaVersion);
        Assert.Equal("default", document.DisplayName);
        Assert.Empty(document.Items);
    }

    [Fact]
    public async Task SaveAsync_CreatesAVersionedJsonDocument()
    {
        var store = CreateStore();

        await store.SaveAsync(new TestConfiguration { DisplayName = "work" });

        Assert.True(store.Exists);
        var content = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains("\"schemaVersion\": 1", content, StringComparison.Ordinal);
        Assert.Contains("\"displayName\": \"work\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsTheDocument()
    {
        var store = CreateStore();

        await store.SaveAsync(new TestConfiguration { DisplayName = "work", Items = ["a", "b"] });
        var loaded = await store.LoadAsync();

        Assert.Equal(TestConfiguration.CurrentSchemaVersion, loaded.SchemaVersion);
        Assert.Equal("work", loaded.DisplayName);
        Assert.Equal(new[] { "a", "b" }, loaded.Items);
    }

    [Fact]
    public async Task LoadAsync_FromANewStoreInstance_ReturnsThePersistedDocument()
    {
        // Simulates an application restart: a brand new service instance reads the file written by the previous one.
        var store = CreateStore();
        await store.SaveAsync(new TestConfiguration { DisplayName = "survivor", Items = ["first", "second"] });

        var restartedStore = CreateStore();
        var loaded = await restartedStore.LoadAsync();

        Assert.True(restartedStore.Exists);
        Assert.Equal("survivor", loaded.DisplayName);
        Assert.Equal(new[] { "first", "second" }, loaded.Items);
    }

    [Fact]
    public async Task SaveAsync_WhenCalledAgain_ReplacesThePreviousDocumentAndLeavesNoTemporaryFile()
    {
        var store = CreateStore();

        await store.SaveAsync(new TestConfiguration { DisplayName = "first" });
        await store.SaveAsync(new TestConfiguration { DisplayName = "second" });
        var loaded = await store.LoadAsync();

        Assert.Equal("second", loaded.DisplayName);
        Assert.Empty(_directory.GetFiles("*.tmp"));
    }

    [Fact]
    public async Task SaveAsync_WhenTheWriterFails_KeepsThePreviousDocument()
    {
        var writer = new FailingAtomicFileWriter(failingCallNumber: 2);
        var store = CreateStore(writer);
        await store.SaveAsync(new TestConfiguration { DisplayName = "first" });

        await Assert.ThrowsAsync<IOException>(
            () => store.SaveAsync(new TestConfiguration { DisplayName = "second" }));

        var loaded = await store.LoadAsync();
        Assert.Equal("first", loaded.DisplayName);
        Assert.Equal(2, writer.CallCount);
    }

    [Fact]
    public async Task LoadAsync_IgnoresTemporaryFilesLeftByAnInterruptedWrite()
    {
        var store = CreateStore();
        await store.SaveAsync(new TestConfiguration { DisplayName = "valid" });
        await File.WriteAllTextAsync(_directory.GetFilePath("settings.json.0123456789abcdef.tmp"), "{ \"schemaVersion\": ");

        var loaded = await store.LoadAsync();

        Assert.Equal("valid", loaded.DisplayName);
    }

    [Fact]
    public async Task SaveAsync_WhenTheDocumentVersionDoesNotMatch_Throws()
    {
        var store = CreateStore();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => store.SaveAsync(new TestConfiguration { SchemaVersion = TestConfiguration.CurrentSchemaVersion + 1 }));

        Assert.Equal("document", exception.ParamName);
        Assert.False(store.Exists);
    }

    [Fact]
    public async Task LoadAsync_WhenTheFileIsNotValidJson_ThrowsConfigurationLoadException()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(store.FilePath, "{ \"schemaVersion\": ");

        var exception = await Assert.ThrowsAsync<ConfigurationLoadException>(() => store.LoadAsync());

        Assert.Equal(store.FilePath, exception.FilePath);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public async Task LoadAsync_WhenTheRootIsNotAnObject_ThrowsConfigurationLoadException()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(store.FilePath, "[]");

        await Assert.ThrowsAsync<ConfigurationLoadException>(() => store.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_WhenTheStoredVersionIsNewer_ThrowsConfigurationSchemaException()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(store.FilePath, "{ \"schemaVersion\": 99 }");

        var exception = await Assert.ThrowsAsync<ConfigurationSchemaException>(() => store.LoadAsync());

        Assert.Equal(99, exception.FoundVersion);
        Assert.Equal(TestConfiguration.CurrentSchemaVersion, exception.SupportedVersion);
    }

    [Fact]
    public async Task LoadAsync_WhenTheVersionIsNotAnInteger_ThrowsConfigurationLoadException()
    {
        var store = CreateStore();
        await File.WriteAllTextAsync(store.FilePath, "{ \"schemaVersion\": \"one\" }");

        await Assert.ThrowsAsync<ConfigurationLoadException>(() => store.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_WhenFileHasUtf8Bom_LoadsDocumentSuccessfully()
    {
        var store = CreateStore();
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes("{ \"schemaVersion\": 1, \"displayName\": \"bom-ok\", \"items\": [\"x\"] }");
        byte[] payloadWithBom = [0xEF, 0xBB, 0xBF, .. jsonBytes];
        await File.WriteAllBytesAsync(store.FilePath, payloadWithBom);

        var loaded = await store.LoadAsync();

        Assert.Equal(1, loaded.SchemaVersion);
        Assert.Equal("bom-ok", loaded.DisplayName);
        Assert.Equal(new[] { "x" }, loaded.Items);
    }

    [Fact]
    public void Constructor_WhenTheDefaultDocumentHasNoVersion_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new JsonConfigurationStore<UnversionedConfiguration>(
                _directory.GetFilePath("settings.json"),
                static () => new UnversionedConfiguration()));
    }

    private JsonConfigurationStore<TestConfiguration> CreateStore(
        IAtomicFileWriter? writer = null,
        IReadOnlyList<IConfigurationMigration>? migrations = null)
        => new(
            _directory.GetFilePath("settings.json"),
            static () => new TestConfiguration(),
            migrations,
            writer);

    private sealed record UnversionedConfiguration : IVersionedDocument
    {
        public int SchemaVersion => 0;
    }
}
