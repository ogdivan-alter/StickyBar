using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using StickyDock.Core.Interaction;
using StickyDock.Interop.Win32;

namespace StickyDock.App.Input;

/// <summary>
/// Reports one region of the window from the pointer position over the whole window footprint (D-E3-01).
/// </summary>
/// <remarks>
/// <para>
/// It holds no policy: it answers a single question —is the pointer inside the window?— and forwards the answer to the
/// region commands of the view model. What to do with the region belongs to <see cref="DockInteractionController"/>
/// (ADR-003, D7 and §8.1).
/// </para>
/// <para>
/// Unlike <see cref="PointerRegionBehavior"/>, which is bound to the pointer events of one element, the report of this
/// region cannot depend on a transition between two elements: the region is the whole footprint, so the pointer is
/// already inside it while it hovers any child. It is therefore reaffirmed on every pointer move —an idempotent
/// operation— and corrected by a reconciliation that asks the system for the real pointer position after the window
/// changes size or position, which is what WPF does not do for a still pointer (defect D-E3-01 of E3).
/// </para>
/// <para>
/// Both are needed because the window is layered (<c>AllowsTransparency</c>): the system hit-tests it per pixel, so a
/// pixel with zero alpha is click-through and no pointer message is delivered there. The reports of this element only
/// arrive over the pixels that paint something —the tab, and the deployed panel— which is exactly the area the region
/// describes; the reconciliation covers the transition, when the panel is still sliding under a pointer that does not
/// move.
/// </para>
/// <para>
/// The reconciliation is a one-shot dispatcher callback, not a timer or a poll: it runs once per geometry change, at
/// input priority, and leaves nothing armed, so the resting cost stays at zero (RNF-02, A6).
/// </para>
/// </remarks>
public static class WindowRegionReporter
{
    /// <summary>
    /// Attached property that holds the region the window footprint represents.
    /// </summary>
    public static readonly DependencyProperty RegionProperty = DependencyProperty.RegisterAttached(
        "Region",
        typeof(DockRegion),
        typeof(WindowRegionReporter),
        new PropertyMetadata(DockRegion.None, OnAffinityChanged));

    /// <summary>
    /// Attached property that holds the command executed when the pointer enters the region.
    /// </summary>
    public static readonly DependencyProperty EnterCommandProperty = DependencyProperty.RegisterAttached(
        "EnterCommand",
        typeof(ICommand),
        typeof(WindowRegionReporter),
        new PropertyMetadata(null, OnAffinityChanged));

    /// <summary>
    /// Attached property that holds the command executed when the pointer leaves the region.
    /// </summary>
    public static readonly DependencyProperty LeaveCommandProperty = DependencyProperty.RegisterAttached(
        "LeaveCommand",
        typeof(ICommand),
        typeof(WindowRegionReporter),
        new PropertyMetadata(null, OnAffinityChanged));

    /// <summary>
    /// Attached property that keeps the state of the reporter of one element. It is private on purpose: it is the
    /// subscription record, not part of the interface of the behavior.
    /// </summary>
    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State",
        typeof(ReporterState),
        typeof(WindowRegionReporter),
        new PropertyMetadata(null));

    /// <summary>
    /// Reads the region attached to an element.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <returns>The attached region.</returns>
    public static DockRegion GetRegion(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (DockRegion)element.GetValue(RegionProperty);
    }

    /// <summary>
    /// Attaches the region to an element.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <param name="value">Region the window footprint represents.</param>
    public static void SetRegion(DependencyObject element, DockRegion value)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.SetValue(RegionProperty, value);
    }

    /// <summary>
    /// Reads the entry command attached to an element.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <returns>The attached command, or <see langword="null"/> when it has none.</returns>
    public static ICommand? GetEnterCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (ICommand?)element.GetValue(EnterCommandProperty);
    }

    /// <summary>
    /// Attaches the entry command to an element.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <param name="value">Command executed when the pointer enters, or <see langword="null"/> to detach it.</param>
    public static void SetEnterCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.SetValue(EnterCommandProperty, value);
    }

    /// <summary>
    /// Reads the exit command attached to an element.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <returns>The attached command, or <see langword="null"/> when it has none.</returns>
    public static ICommand? GetLeaveCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (ICommand?)element.GetValue(LeaveCommandProperty);
    }

    /// <summary>
    /// Attaches the exit command to an element.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <param name="value">Command executed when the pointer leaves, or <see langword="null"/> to detach it.</param>
    public static void SetLeaveCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.SetValue(LeaveCommandProperty, value);
    }

    private static void OnAffinityChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not FrameworkElement target)
        {
            return;
        }

        // Subscribed and unsubscribed every time so that the lifetime of the reporter follows the three properties: with
        // a region and both commands it works, and without any of them it leaves no handler behind.
        Detach(target);

        if (GetRegion(target) == DockRegion.None || GetEnterCommand(target) is null || GetLeaveCommand(target) is null)
        {
            return;
        }

        Attach(target);
    }

    private static void Attach(FrameworkElement target)
    {
        var state = new ReporterState(target);

        target.SetValue(StateProperty, state);

        target.Loaded += state.OnLoaded;
        target.Unloaded += state.OnUnloaded;
        target.MouseMove += state.OnMouseMove;
        target.MouseEnter += state.OnMouseEnter;
        target.MouseLeave += state.OnMouseLeave;
    }

    private static void Detach(FrameworkElement target)
    {
        if (target.GetValue(StateProperty) is not ReporterState state)
        {
            return;
        }

        target.Loaded -= state.OnLoaded;
        target.Unloaded -= state.OnUnloaded;
        target.MouseMove -= state.OnMouseMove;
        target.MouseEnter -= state.OnMouseEnter;
        target.MouseLeave -= state.OnMouseLeave;

        state.Detach();

        target.ClearValue(StateProperty);
    }

    private static void Execute(ICommand? command, DockRegion region)
    {
        if (command is null || !command.CanExecute(region))
        {
            return;
        }

        command.Execute(region);
    }

    /// <summary>
    /// Subscription record of one element: the tracker that decides what to report, the window whose geometry is watched
    /// and the flag that coalesces the reconciliation into a single dispatcher callback.
    /// </summary>
    private sealed class ReporterState
    {
        private readonly FrameworkElement _element;
        private readonly WindowRegionTracker _tracker = new();

        private Window? _window;
        private bool _reconcileQueued;
        private bool _detached;

        internal ReporterState(FrameworkElement element) => _element = element;

        internal void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Loaded can arrive more than once for the same element, so the subscription is rebuilt, never duplicated.
            UnsubscribeFromWindow();

            _window = Window.GetWindow(_element);

            if (_window is not null)
            {
                _window.SizeChanged += OnWindowSizeChanged;
                _window.LocationChanged += OnWindowLocationChanged;
            }

            RequestReconcile();
        }

        internal void OnUnloaded(object sender, RoutedEventArgs e) => Detach();

        internal void OnMouseMove(object sender, MouseEventArgs e) => Report(_tracker.Observe(true));

        internal void OnMouseEnter(object sender, MouseEventArgs e) => Report(_tracker.Observe(true));

        internal void OnMouseLeave(object sender, MouseEventArgs e)
        {
            Report(_tracker.Observe(false));

            // WPF can report the exit while the window is still growing or the content is still sliding under a pointer
            // that has not moved, so the report is checked against the real cursor before the grace period decides. This
            // is what keeps the deployed panel from being abandoned mid-slide (D-E3-01).
            RequestReconcile();
        }

        internal void Detach()
        {
            _detached = true;
            UnsubscribeFromWindow();
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e) => RequestReconcile();

        private void OnWindowLocationChanged(object? sender, EventArgs e) => RequestReconcile();

        /// <summary>
        /// Queues one reconciliation, whatever caused it: the window changes size and position in the same operation and
        /// several messages arrive for one transition.
        /// </summary>
        private void RequestReconcile()
        {
            if (_reconcileQueued)
            {
                return;
            }

            _reconcileQueued = true;

            _ = _element.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(Reconcile));
        }

        private void Reconcile()
        {
            _reconcileQueued = false;

            if (_detached)
            {
                return;
            }

            var window = _window ?? Window.GetWindow(_element);

            if (window is null)
            {
                return;
            }

            var handle = new WindowInteropHelper(window).Handle;

            if (handle == nint.Zero)
            {
                return;
            }

            var bounds = WindowPlacement.GetBounds(handle);

            if (bounds.IsEmpty || !CursorProbe.TryGetPosition(out var x, out var y))
            {
                return;
            }

            var inside = bounds.Contains(x, y);

            // No two observations can disagree about a transition: the tracker answers None while the pointer stays on
            // one side, so asking again after every geometry change is free and cannot disturb the single timer slot.
            Report(_tracker.Observe(inside));
        }

        private void Report(RegionReport report)
        {
            switch (report)
            {
                case RegionReport.Enter:
                    Execute(GetEnterCommand(_element), GetRegion(_element));
                    break;

                case RegionReport.Leave:
                    Execute(GetLeaveCommand(_element), GetRegion(_element));
                    break;
            }
        }

        private void UnsubscribeFromWindow()
        {
            var window = _window;

            if (window is null)
            {
                return;
            }

            _window = null;

            window.SizeChanged -= OnWindowSizeChanged;
            window.LocationChanged -= OnWindowLocationChanged;
        }
    }
}
