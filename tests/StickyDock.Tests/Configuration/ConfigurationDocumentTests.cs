using StickyDock.Core.Configuration;
using StickyDock.Core.Interaction;
using StickyDock.Tests.Docking;
using StickyDock.Tests.TestInfrastructure;

namespace StickyDock.Tests.Configuration;

/// <summary>
/// ADR-003, D1 and D-04: the document is the only writer of the configuration, it keeps at most one write in flight,
/// coalesces the changes that arrive meanwhile (latest wins) and survives a failing store without closing the session.
/// These tests pin the behaviour that the docking coordinator, the interaction controller and E6 depend on.
/// </summary>
public sealed class ConfigurationDocumentTests
{
    private static readonly TimeSpan Guard = TimeSpan.FromSeconds(5);

    [Fact]
    public void Current_ReturnsTheInitialDocument()
    {
        var initial = new StickyDockConfiguration { GracePeriodMilliseconds = 350 };

        using var document = CreateDocument(new RecordingConfigurationStore(), initial);

        Assert.Same(initial, document.Current);
    }

    [Fact]
    public void Update_ReplacesCurrent()
    {
        using var document = CreateDocument(new RecordingConfigurationStore());

        var changed = document.Update(configuration => configuration with { GracePeriodMilliseconds = 300 });

        Assert.True(changed);
        Assert.Equal(300, document.Current.GracePeriodMilliseconds);
    }

    [Fact]
    public void Update_PersistsThroughTheStore()
    {
        var store = new RecordingConfigurationStore();

        using var document = CreateDocument(store);

        document.Update(configuration => configuration with { DockEdge = null });

        var saved = Assert.Single(store.Saved);
        Assert.Null(saved.DockEdge);
        Assert.Equal(StickyDockConfiguration.CurrentSchemaVersion, saved.SchemaVersion);
    }

    [Fact]
    public void Update_RaisesChangedOnceWithTheNewDocument()
    {
        using var document = CreateDocument(new RecordingConfigurationStore());
        var raised = new List<StickyDockConfiguration>();
        document.Changed += (_, arguments) => raised.Add(arguments.Document);

        document.Update(configuration => configuration with { GracePeriodMilliseconds = 500 });

        var only = Assert.Single(raised);
        Assert.Equal(500, only.GracePeriodMilliseconds);
        Assert.Equal(500, document.Current.GracePeriodMilliseconds);
    }

    [Fact]
    public void Update_WithAnEqualDocument_ReturnsFalseAndDoesNotWrite()
    {
        var store = new RecordingConfigurationStore();

        using var document = CreateDocument(store);

        var changed = document.Update(configuration => configuration);

        Assert.False(changed);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public void Update_WhenTheTransformationReturnsNoDocument_ThrowsAndKeepsTheCurrentOne()
    {
        var store = new RecordingConfigurationStore();

        using var document = CreateDocument(store);

        Assert.Throws<InvalidOperationException>(
            () => document.Update(_ => null!));
        Assert.Equal(GracePeriod.DefaultMilliseconds, document.Current.GracePeriodMilliseconds);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public void Update_WhenTheTransformationIsMissing_Throws()
    {
        using var document = CreateDocument(new RecordingConfigurationStore());

        Assert.Throws<ArgumentNullException>(() => document.Update(null!));
    }

    [Fact]
    public async Task FlushAsync_WhenNoWriteIsPending_CompletesImmediately()
    {
        using var document = CreateDocument(new RecordingConfigurationStore());

        await document.FlushAsync().WaitAsync(Guard);
    }

    [Fact]
    public async Task FlushAsync_WaitsForTheWriteInFlight()
    {
        var store = new BlockingConfigurationStore();

        using var document = CreateDocument(store);

        document.Update(configuration => configuration with { GracePeriodMilliseconds = 300 });

        var flush = document.FlushAsync();
        Assert.False(flush.IsCompleted);

        store.Open();
        await flush.WaitAsync(Guard);

        Assert.Equal(new[] { 300 }, SavedGracePeriods(store.Saved));
    }

    [Fact]
    public async Task ConcurrentUpdates_CoalesceIntoOneWrite()
    {
        var store = new BlockingConfigurationStore();

        using var document = CreateDocument(store);

        document.Update(configuration => configuration with { GracePeriodMilliseconds = 500 });
        document.Update(configuration => configuration with { GracePeriodMilliseconds = 350 });
        document.Update(configuration => configuration with { GracePeriodMilliseconds = 300 });

        // Only the first document reached the store; the two changes that arrived meanwhile were coalesced.
        Assert.Equal(new[] { 500 }, SavedGracePeriods(store.Saved));

        var flush = document.FlushAsync();
        store.Open();
        await flush.WaitAsync(Guard);

        // Latest wins: the intermediate 350 ms is never written.
        Assert.Equal(new[] { 500, 300 }, SavedGracePeriods(store.Saved));
        Assert.Equal(300, document.Current.GracePeriodMilliseconds);
    }

    [Fact]
    public void Update_WhenTheStoreThrows_KeepsTheSessionAliveAndRetriesOnTheNextChange()
    {
        var store = new ThrowingConfigurationStore();
        var logger = new RecordingLogger();

        using var document = new ConfigurationDocument(store, new StickyDockConfiguration(), logger);

        document.Update(configuration => configuration with { GracePeriodMilliseconds = 300 });
        document.Update(configuration => configuration with { GracePeriodMilliseconds = 320 });

        Assert.Equal(2, store.SaveCount);
        Assert.Equal(320, document.Current.GracePeriodMilliseconds);
        Assert.Contains(logger.Entries, entry => entry.Contains("could not be written", StringComparison.Ordinal));
    }

    [Fact]
    public void Update_AfterDispose_Throws()
    {
        var document = CreateDocument(new RecordingConfigurationStore());
        document.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => document.Update(configuration => configuration with { GracePeriodMilliseconds = 300 }));
    }

    [Fact]
    public async Task FlushAsync_AfterDispose_Throws()
    {
        var document = CreateDocument(new RecordingConfigurationStore());
        document.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => document.FlushAsync());
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        // The container disposes the same instance once per registration key it was registered under.
        var document = CreateDocument(new RecordingConfigurationStore());

        document.Dispose();
        document.Dispose();
    }

    [Fact]
    public void Dispose_WhileAWriteIsPending_CompletesItWithinTheBoundedWait()
    {
        var store = new BlockingConfigurationStore();
        var document = CreateDocument(store);
        document.Update(configuration => configuration with { GracePeriodMilliseconds = 300 });

        store.Open();
        document.Dispose();

        Assert.Equal(new[] { 300 }, SavedGracePeriods(store.Saved));
    }

    [Fact]
    public async Task Dispose_WithACoalescedChange_WritesTheLatestDocument()
    {
        // D-04: the change that arrived while a write was in flight is dirty when the process closes, and shutdown
        // drains it instead of leaving the previous value on disk.
        var store = new FirstWriteGatedConfigurationStore();
        using var document = CreateDocument(store);

        document.Update(configuration => configuration with { GracePeriodMilliseconds = 300 });
        document.Update(configuration => configuration with { GracePeriodMilliseconds = 320 });

        // Only the first document reached the store; the second change was coalesced behind the write in flight.
        Assert.Equal(new[] { 300 }, SavedGracePeriods(store.Saved));

        var dispose = Task.Run(document.Dispose);

        // Dispose marks the document as closed before waiting on the write in flight, so refusing a change is the
        // signal that the coalesced write will run while the document is already disposed.
        Assert.True(SpinWait.SpinUntil(() => DisposeHasStarted(document), Guard));

        store.ReleaseFirstWrite();

        await dispose.WaitAsync(Guard);

        Assert.Equal(new[] { 300, 320 }, SavedGracePeriods(store.Saved));
        Assert.Equal(320, document.Current.GracePeriodMilliseconds);
    }

    [Fact]
    public void Constructor_WhenAnArgumentIsMissing_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfigurationDocument(null!, new StickyDockConfiguration(), new RecordingLogger()));
        Assert.Throws<ArgumentNullException>(() => new ConfigurationDocument(new RecordingConfigurationStore(), null!, new RecordingLogger()));
        Assert.Throws<ArgumentNullException>(() => new ConfigurationDocument(new RecordingConfigurationStore(), new StickyDockConfiguration(), null!));
    }

    private static ConfigurationDocument CreateDocument(
        IConfigurationStore<StickyDockConfiguration> store,
        StickyDockConfiguration? initial = null)
        => new(store, initial ?? new StickyDockConfiguration(), new RecordingLogger());

    private static int[] SavedGracePeriods(IReadOnlyList<StickyDockConfiguration> saved)
        => [.. saved.Select(document => document.GracePeriodMilliseconds)];

    /// <summary>
    /// Checks whether <see cref="ConfigurationDocument.Dispose"/> has already closed the document, by observing the
    /// documented side effect of that flag: the transformation is refused. The transformation is the identity, so the
    /// probe has no effect while the document is still open.
    /// </summary>
    private static bool DisposeHasStarted(ConfigurationDocument document)
    {
        try
        {
            document.Update(configuration => configuration);
            return false;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    /// <summary>
    /// Store whose writes stay pending until the test opens the gate, which is what makes the single-write-in-flight and
    /// the coalescing observable without waiting on real input/output.
    /// </summary>
    private sealed class BlockingConfigurationStore : IConfigurationStore<StickyDockConfiguration>
    {
        private readonly List<StickyDockConfiguration> _saved = [];
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReadOnlyList<StickyDockConfiguration> Saved => _saved;

        public string FilePath => @"memory:\blocking-settings.json";

        public bool Exists => true;

        public Task<StickyDockConfiguration> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new StickyDockConfiguration());

        public Task SaveAsync(StickyDockConfiguration document, CancellationToken cancellationToken = default)
        {
            _saved.Add(document);
            return _gate.Task;
        }

        public void Open() => _gate.TrySetResult();
    }

    /// <summary>
    /// Store whose first write stays pending until the test releases it and whose later writes complete immediately,
    /// which makes a change coalesced during shutdown observable without waiting on real input/output.
    /// </summary>
    private sealed class FirstWriteGatedConfigurationStore : IConfigurationStore<StickyDockConfiguration>
    {
        private readonly Lock _gate = new();
        private readonly List<StickyDockConfiguration> _saved = [];
        private readonly TaskCompletionSource _firstWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReadOnlyList<StickyDockConfiguration> Saved
        {
            get
            {
                lock (_gate)
                {
                    return [.. _saved];
                }
            }
        }

        public string FilePath => @"memory:\gated-settings.json";

        public bool Exists => true;

        public Task<StickyDockConfiguration> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new StickyDockConfiguration());

        public Task SaveAsync(StickyDockConfiguration document, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _saved.Add(document);
                return _saved.Count == 1 ? _firstWrite.Task : Task.CompletedTask;
            }
        }

        public void ReleaseFirstWrite() => _firstWrite.TrySetResult();
    }

    /// <summary>
    /// Store that fails every write, to check that a failure does not close the session nor lose the document in memory.
    /// </summary>
    private sealed class ThrowingConfigurationStore : IConfigurationStore<StickyDockConfiguration>
    {
        public int SaveCount { get; private set; }

        public string FilePath => @"memory:\throwing-settings.json";

        public bool Exists => false;

        public Task<StickyDockConfiguration> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new StickyDockConfiguration());

        public Task SaveAsync(StickyDockConfiguration document, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            throw new IOException("The disk is not available.");
        }
    }
}
