using StickyBar.Core.Diagnostics;

namespace StickyBar.Core.Configuration;

/// <summary>
/// Default implementation of <see cref="IConfigurationDocument"/>: the single writer of the configuration (ADR-003, D1).
/// </summary>
/// <remarks>
/// <para>
/// It keeps at most one write in flight and coalesces the changes that arrive meanwhile, so the file never receives an
/// intermediate state (latest wins) and the UI thread is never blocked by input/output (D-04).
/// </para>
/// <para>
/// <see cref="Update"/> is meant for the UI thread and takes a short lock; <see cref="FlushAsync"/> and
/// <see cref="Dispose"/> may be called from any thread.
/// </para>
/// </remarks>
public sealed class ConfigurationDocument : IConfigurationDocument, IDisposable
{
    private static readonly TimeSpan ShutdownWriteTimeout = TimeSpan.FromSeconds(2);

    private readonly IConfigurationStore<StickyBarConfiguration> _store;
    private readonly ILogger _logger;
    private readonly Lock _gate = new();

    private StickyBarConfiguration _current;
    private TaskCompletionSource? _writerCompletion;
    private bool _dirty;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationDocument"/> class.
    /// </summary>
    /// <param name="store">Store that receives the writes. Not owned.</param>
    /// <param name="initial">Document loaded at startup.</param>
    /// <param name="logger">Logger of the document.</param>
    public ConfigurationDocument(
        IConfigurationStore<StickyBarConfiguration> store,
        StickyBarConfiguration initial,
        ILogger logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _current = initial ?? throw new ArgumentNullException(nameof(initial));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public StickyBarConfiguration Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<ConfigurationChangedEventArgs>? Changed;

    /// <inheritdoc />
    public bool Update(Func<StickyBarConfiguration, StickyBarConfiguration> change)
    {
        ArgumentNullException.ThrowIfNull(change);

        StickyBarConfiguration updated;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var candidate = change(_current)
                ?? throw new InvalidOperationException("The configuration transformation returned no document.");

            if (candidate.Equals(_current))
            {
                return false;
            }

            _current = candidate;
            _dirty = true;
            updated = candidate;
        }

        // Raised outside the lock: a subscriber may read Current or ask for another change without deadlocking.
        Changed?.Invoke(this, new ConfigurationChangedEventArgs(updated));

        StartWriteIfNeeded();

        return true;
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            Task? pending;

            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                pending = _writerCompletion?.Task;
            }

            if (pending is null)
            {
                return;
            }

            await pending.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Drains the writes before closing: it waits for the write in flight and also writes the change that arrived
    /// meanwhile, so the file left on disk is the latest document (D-04).
    /// </summary>
    /// <remarks>
    /// The wait is bounded so shutdown never hangs; when it expires the error is logged and the application closes
    /// anyway, in which case the last preference may be lost. Once disposed, <see cref="Update"/> and
    /// <see cref="FlushAsync"/> throw <see cref="ObjectDisposedException"/>.
    /// </remarks>
    public void Dispose()
    {
        StartWriteIfNeeded();

        Task? pending;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            pending = _writerCompletion?.Task;
        }

        Changed = null;

        if (pending is not null)
        {
            try
            {
                if (!pending.Wait(ShutdownWriteTimeout))
                {
                    _logger.Error(
                        $"The pending configuration write did not finish within {ShutdownWriteTimeout.TotalSeconds} s; " +
                        "the preference may be lost.");
                }
            }
            catch (AggregateException exception)
            {
                _logger.Error("The pending configuration write failed during shutdown.", exception.GetBaseException());
            }
        }

        GC.SuppressFinalize(this);
    }

    private void StartWriteIfNeeded()
    {
        TaskCompletionSource completion;
        StickyBarConfiguration document;

        lock (_gate)
        {
            if (_disposed || !_dirty || _writerCompletion is not null)
            {
                return;
            }

            _dirty = false;
            document = _current;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _writerCompletion = completion;
        }

        // Started inline, not on the thread pool: a synchronous store has already written when Update returns.
        _ = WriteLoopAsync(completion, document);
    }

    private async Task WriteLoopAsync(TaskCompletionSource completion, StickyBarConfiguration document)
    {
        while (true)
        {
            try
            {
                await _store.SaveAsync(document).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // The session keeps working; the next change retries. Only the preference of this change may be lost.
                _logger.Error($"The configuration could not be written to '{_store.FilePath}'.", exception);
            }

            lock (_gate)
            {
                // Deliberately not checking _disposed: a change coalesced during the last write must still reach the
                // disk, which is what Dispose promises (D-04). Update refuses changes once disposed, so the loop can
                // run at most one more iteration and always terminates.
                if (!_dirty)
                {
                    _writerCompletion = null;
                    break;
                }

                document = _current;
                _dirty = false;
            }
        }

        completion.TrySetResult();
    }
}
