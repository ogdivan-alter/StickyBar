using StickyDock.Core.Configuration;
using StickyDock.Core.Diagnostics;
using StickyDock.Core.Services;
using StickyDock.Core.Timing;

namespace StickyDock.Core.Interaction;

/// <summary>
/// Owns the interaction state of one bar: the regions the pointer is in, the single timer slot and the transitions
/// between <see cref="DockState"/> values (ADR-003, D4 and D7).
/// </summary>
/// <remarks>
/// <para>
/// It is the single implementation of <see cref="IDockStateService"/> and of <see cref="IDockRegionSink"/>, so the state
/// contract does not grow with the pointer input. Its only caller beyond the view model is the composition root, which
/// wires <see cref="BeginDrag"/> and <see cref="EndDrag"/> to the docking coordinator.
/// </para>
/// <para>
/// Single-threaded: every member must be called on the UI thread and the scheduler delivers its callbacks there. The
/// class takes no locks and must not be shared between threads.
/// </para>
/// </remarks>
public sealed class DockInteractionController : IDockStateService, IDockRegionSink, IDisposable
{
    private readonly ISingleShotScheduler _scheduler;
    private readonly IConfigurationDocument _configuration;
    private readonly ILogger _logger;
    private readonly HashSet<DockRegion> _regions = [];

    private PendingTimer _pending = PendingTimer.None;
    private DockState _state = DockState.Tab;
    private bool _dragging;
    private bool _isPinned;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockInteractionController"/> class.
    /// </summary>
    /// <param name="scheduler">Single slot used for every timer of the machine.</param>
    /// <param name="configuration">Document the grace period is read from when a timer is armed.</param>
    /// <param name="logger">Logger of the controller.</param>
    public DockInteractionController(
        ISingleShotScheduler scheduler,
        IConfigurationDocument configuration,
        ILogger logger)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public DockState State => _state;

    /// <summary>
    /// Gets a value indicating whether a drag of the bar is in progress, in which case the automatic transitions are
    /// suppressed.
    /// </summary>
    public bool IsDragging => _dragging;

    /// <inheritdoc />
    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_isPinned == value)
            {
                return;
            }

            _isPinned = value;
            _logger.Debug(_isPinned ? "Dock bar pinned open." : "Dock bar unpinned.");

            if (_isPinned)
            {
                if (_state == DockState.Collapsing)
                {
                    CancelPendingTimer();
                    SetState(DockState.Expanded);
                }
                else if (_state == DockState.Expanded)
                {
                    CancelPendingTimer();
                }
            }

            Reevaluate();
        }
    }

    /// <summary>
    /// Gets the regions the pointer is in. Diagnostic and test surface.
    /// </summary>
    public IReadOnlyCollection<DockRegion> ActiveRegions => _regions.ToArray();

    /// <inheritdoc />
    public event EventHandler<DockStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public void EnterRegion(DockRegion region)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (region == DockRegion.None)
        {
            return;
        }

        // ADR-004 D3: a region whose element does not paint in this state cannot be entered. The rule is what makes a
        // late or duplicated pointer event harmless, and it is the only guard that does not depend on a matching exit.
        if (!DockRegionPresence.IsPresent(region, _state))
        {
            _logger.Debug($"Ignored region {region}: it is not present in state {_state}.");
            return;
        }

        if (!_regions.Add(region))
        {
            if (_state == DockState.Tab && region == DockRegion.Tab && _pending == PendingTimer.None)
            {
                Reevaluate();
            }

            return;
        }

        _logger.Debug($"Pointer entered region {region}.");

        // ADR-004 D2: only the content reverses the slide-out. Coming back to the tab is not coming back to the bar:
        // the settle completes and the hover of the tab —200 ms, E3-CA1— starts a new activation.
        if (_state == DockState.Collapsing && DockRegionPresence.IsContent(region))
        {
            _scheduler.Cancel();
            _pending = PendingTimer.None;
            SetState(DockState.Expanded);
            return;
        }

        if (_state == DockState.Collapsing)
        {
            // The tab entered while the bar slides out: nothing to decide, the settle owns the transition.
            return;
        }

        Reevaluate();
    }

    /// <inheritdoc />
    public void LeaveRegion(DockRegion region)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_regions.Remove(region))
        {
            return;
        }

        _logger.Debug($"Pointer left region {region}.");

        Reevaluate();
    }

    /// <summary>
    /// Suppresses the automatic transitions until <see cref="EndDrag"/> and cancels any pending timer.
    /// </summary>
    public void BeginDrag()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _dragging = true;

        // Neither Arming nor Collapsing has a stable footprint, and cancelling their timer would strand the bar in a
        // transient state with nothing left to resolve it, so both settle first.
        SettleToStableState();
        CancelPendingTimer();
    }

    /// <summary>
    /// Reenables the automatic transitions and reevaluates with the regions as they are now.
    /// </summary>
    public void EndDrag()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _dragging = false;
        Reevaluate();
    }

    /// <inheritdoc />
    public void Expand()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancelPendingTimer();
        SetState(DockState.Expanded);
    }

    /// <inheritdoc />
    public void CollapseToTab()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancelPendingTimer();
        SetState(DockState.Tab);
    }

    /// <summary>
    /// Cancels the pending timer. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancelPendingTimer();
        _regions.Clear();
        StateChanged = null;
    }

    /// <summary>
    /// Single routine every input goes through (ADR-003, §7.3), so no two paths can disagree.
    /// </summary>
    private void Reevaluate()
    {
        if (_dragging)
        {
            return;
        }

        if (_state == DockState.Collapsing)
        {
            // The settle timer or an incoming region resolves it; there is nothing to decide meanwhile.
            return;
        }

        if (_state == DockState.Expanded)
        {
            if (_regions.Count == 0 && !_isPinned)
            {
                if (_pending != PendingTimer.Grace)
                {
                    var grace = InteractionTimings.FromGrace(_configuration.Current.GracePeriodMilliseconds).Grace;
                    Arm(PendingTimer.Grace, grace, OnGraceElapsed);
                }
            }
            else
            {
                CancelPendingTimer();
            }

            return;
        }

        if (_state == DockState.Arming)
        {
            if (_regions.Contains(DockRegion.Tab))
            {
                // The activation keeps running; the pointer never left the tab.
                return;
            }

            // ADR-004 D3: the tab is the only region that can exist in Arming, because the content is not painted yet,
            // so its absence leaves the set empty and the activation has nothing left to deploy for (E3-CA4).
            CancelPendingTimer();
            SetState(DockState.Tab);
            return;
        }

        // DockState.Tab
        if (!_regions.Contains(DockRegion.Tab))
        {
            CancelPendingTimer();
            return;
        }

        // Hovering the tab arms the activation: the micro-expansion has a state of its own so that it can be seen
        // before the bar is completely deployed (HU02 CA #2, E3-CA1).
        SetState(DockState.Arming);
        Arm(PendingTimer.Activation, TimeSpan.FromMilliseconds(InteractionTimings.ActivationMilliseconds), OnActivationElapsed);
    }

    private void OnActivationElapsed()
    {
        if (_pending != PendingTimer.Activation)
        {
            return;
        }

        _pending = PendingTimer.None;

        if (_dragging || _state != DockState.Arming)
        {
            return;
        }

        _logger.Debug("The activation timer elapsed; deploying the bar.");
        SetState(DockState.Expanded);
    }

    private void OnGraceElapsed()
    {
        if (_pending != PendingTimer.Grace)
        {
            return;
        }

        _pending = PendingTimer.None;

        if (_dragging || _isPinned || _state != DockState.Expanded)
        {
            return;
        }

        _logger.Debug("The grace period elapsed; collapsing the bar.");
        Arm(PendingTimer.Settle, TimeSpan.FromMilliseconds(InteractionTimings.SlideMilliseconds), OnSettleElapsed);
        SetState(DockState.Collapsing);
    }

    private void OnSettleElapsed()
    {
        if (_pending != PendingTimer.Settle)
        {
            return;
        }

        _pending = PendingTimer.None;

        if (_state != DockState.Collapsing)
        {
            return;
        }

        SetState(DockState.Tab);

        // The pointer may already be back on the tab, in which case a new activation starts right away.
        Reevaluate();
    }

    private void SettleToStableState()
    {
        if (_state == DockState.Arming)
        {
            SetState(DockState.Tab);
        }
        else if (_state == DockState.Collapsing)
        {
            SetState(DockState.Tab);
        }
    }

    private void Arm(PendingTimer timer, TimeSpan delay, Action callback)
    {
        _pending = timer;
        _scheduler.Schedule(delay, callback);
    }

    private void CancelPendingTimer()
    {
        _pending = PendingTimer.None;
        _scheduler.Cancel();
    }

    private void SetState(DockState newState)
    {
        if (_state == newState)
        {
            return;
        }

        _state = newState;

        // ADR-004 D3: the content regions stop existing the moment the bar leaves the deployed footprint, whether or
        // not their exit arrived. Purging before the event leaves every subscriber —and every reevaluation the event
        // triggers— looking at a set that is already consistent with the new state.
        PurgeAbsentRegions();

        _logger.Debug($"Dock state changed to {newState}.");
        StateChanged?.Invoke(this, new DockStateChangedEventArgs(newState));
    }

    private void PurgeAbsentRegions()
    {
        if (_regions.Count == 0)
        {
            return;
        }

        foreach (var region in _regions.Where(region => !DockRegionPresence.IsPresent(region, _state)).ToArray())
        {
            _regions.Remove(region);
            _logger.Debug($"Dropped region {region}: it is not present in state {_state}.");
        }
    }

    /// <summary>
    /// Kind of timer armed in the single slot. The callbacks are shared and check it before acting, so a callback that
    /// arrives after the slot was rearmed for a different timer is ignored.
    /// </summary>
    private enum PendingTimer
    {
        None = 0,
        Activation = 1,
        Grace = 2,
        Settle = 3,
    }
}
