using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using StickyDock.Core.Diagnostics;

namespace StickyDock.Core.Configuration;

/// <summary>
/// JSON implementation of <see cref="IConfigurationStore{TDocument}"/> with schema versioning, forward migrations and
/// atomic writes.
/// </summary>
/// <typeparam name="TDocument">Document type persisted by the store.</typeparam>
public sealed class JsonConfigurationStore<TDocument> : IConfigurationStore<TDocument>
    where TDocument : class, IVersionedDocument
{
    /// <summary>
    /// Name of the property that carries the schema version in the serialized document.
    /// </summary>
    public const string SchemaVersionPropertyName = "schemaVersion";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Func<TDocument> _createDefault;
    private readonly IReadOnlyList<IConfigurationMigration> _migrations;
    private readonly IAtomicFileWriter _fileWriter;
    private readonly ILogger? _logger;
    private readonly int _targetSchemaVersion;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonConfigurationStore{TDocument}"/> class.
    /// </summary>
    /// <param name="filePath">Full or relative path of the configuration file.</param>
    /// <param name="createDefault">Factory of the default document, used when no file exists yet.</param>
    /// <param name="migrations">Migrations that upgrade older schema versions, in any order.</param>
    /// <param name="fileWriter">Atomic writer used for persistence; the default one is used when omitted.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public JsonConfigurationStore(
        string filePath,
        Func<TDocument> createDefault,
        IReadOnlyList<IConfigurationMigration>? migrations = null,
        IAtomicFileWriter? fileWriter = null,
        ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A configuration file path is required.", nameof(filePath));
        }

        ArgumentNullException.ThrowIfNull(createDefault);

        FilePath = Path.GetFullPath(filePath);
        _createDefault = createDefault;
        _migrations = migrations ?? [];
        _fileWriter = fileWriter ?? new AtomicFileWriter();
        _logger = logger;

        // The supported version is taken from the default document so that the store and the document cannot drift.
        _targetSchemaVersion = createDefault().SchemaVersion;

        if (_targetSchemaVersion < 1)
        {
            throw new InvalidOperationException("The default document must declare a schema version of at least 1.");
        }
    }

    /// <inheritdoc />
    public string FilePath { get; }

    /// <inheritdoc />
    public bool Exists => File.Exists(FilePath);

    /// <inheritdoc />
    public async Task<TDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Exists)
        {
            _logger?.Information($"No configuration file at '{FilePath}'; defaults are used.");
            return _createDefault();
        }

        byte[] payload;

        try
        {
            payload = await File.ReadAllBytesAsync(FilePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ConfigurationLoadException(FilePath, "The file could not be read.", exception);
        }

        return Parse(payload);
    }

    /// <inheritdoc />
    public async Task SaveAsync(TDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.SchemaVersion != _targetSchemaVersion)
        {
            throw new ArgumentException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The document declares schema version {document.SchemaVersion} but this application writes version {_targetSchemaVersion}."),
                nameof(document));
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(document, SerializerOptions);

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _fileWriter.WriteAsync(FilePath, payload, cancellationToken).ConfigureAwait(false);
            _logger?.Debug($"Configuration written to '{FilePath}'.");
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private TDocument Parse(byte[] payload)
    {
        ReadOnlySpan<byte> utf8Json = payload;

        // Tolerate UTF-8 BOM (0xEF, 0xBB, 0xBF) written by external editors (D-08).
        if (utf8Json.Length >= 3 && utf8Json[0] == 0xEF && utf8Json[1] == 0xBB && utf8Json[2] == 0xBF)
        {
            utf8Json = utf8Json[3..];
        }

        JsonNode? node;

        try
        {
            node = JsonNode.Parse(utf8Json);
        }
        catch (JsonException exception)
        {
            throw new ConfigurationLoadException(FilePath, "The file does not contain valid JSON.", exception);
        }

        if (node is not JsonObject document)
        {
            throw new ConfigurationLoadException(FilePath, "The root of the document must be a JSON object.");
        }

        var storedVersion = ReadSchemaVersion(document);

        if (storedVersion > _targetSchemaVersion)
        {
            throw new ConfigurationSchemaException(FilePath, storedVersion, _targetSchemaVersion);
        }

        var migrated = ApplyMigrations(document, storedVersion);
        TDocument? configuration;

        try
        {
            configuration = migrated.Deserialize<TDocument>(SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new ConfigurationLoadException(FilePath, "The document could not be deserialized.", exception);
        }

        if (configuration is null)
        {
            throw new ConfigurationLoadException(FilePath, "The document produced an empty configuration.");
        }

        if (configuration.SchemaVersion != _targetSchemaVersion)
        {
            throw new ConfigurationSchemaException(FilePath, configuration.SchemaVersion, _targetSchemaVersion);
        }

        return configuration;
    }

    private int ReadSchemaVersion(JsonObject document)
    {
        if (!document.TryGetPropertyValue(SchemaVersionPropertyName, out var node) || node is null)
        {
            // A document without a version is treated as the oldest one and must be migrated.
            return 0;
        }

        if (node is JsonValue value && value.TryGetValue<int>(out var version) && version >= 0)
        {
            return version;
        }

        throw new ConfigurationLoadException(
            FilePath,
            $"The '{SchemaVersionPropertyName}' property must be a non-negative integer.");
    }

    private JsonObject ApplyMigrations(JsonObject document, int storedVersion)
    {
        var workingDocument = document.DeepClone().AsObject();
        var version = storedVersion;

        while (version < _targetSchemaVersion)
        {
            var migration = _migrations.FirstOrDefault(candidate => candidate.FromVersion == version)
                ?? throw new ConfigurationSchemaException(
                    FilePath,
                    storedVersion,
                    _targetSchemaVersion,
                    string.Create(CultureInfo.InvariantCulture, $"No migration is registered from version {version} to {version + 1}."));

            migration.Apply(workingDocument);
            workingDocument[SchemaVersionPropertyName] = version + 1;

            _logger?.Debug(string.Create(CultureInfo.InvariantCulture, $"Configuration migrated from schema version {version}."));

            version++;
        }

        return workingDocument;
    }
}
