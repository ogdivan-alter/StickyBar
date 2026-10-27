using StickyDock.Core.Diagnostics;

namespace StickyDock.Core.Services;

/// <summary>
/// Default implementation of <see cref="IDockStateService"/>.
/// </summary>
public sealed class DockStateService : IDockStateService
{
    private readonly ILogger _logger;
    private DockState _state = DockState.Tab;
    private readonly object _sync = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DockStateService"/> class.
    /// </summary>
    public DockStateService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public DockState State
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<DockStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public void CollapseToTab()
    {
        SetState(DockState.Tab);
    }

    /// <inheritdoc />
    public void Expand()
    {
        SetState(DockState.Expanded);
    }

    private void SetState(DockState newState)
    {
        bool changed;

        lock (_sync)
        {
            changed = _state != newState;
            if (changed)
            {
                _state = newState;
            }
        }

        if (changed)
        {
            _logger.Debug($"Dock state changed to {newState}.");
            StateChanged?.Invoke(this, new DockStateChangedEventArgs(newState));
        }
    }
}
