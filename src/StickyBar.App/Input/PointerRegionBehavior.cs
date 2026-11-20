using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using StickyBar.Core.Interaction;

namespace StickyBar.App.Input;

/// <summary>
/// Turns the pointer events of one element into the region commands of the view model (ADR-003, D7).
/// </summary>
/// <remarks>
/// <para>
/// It holds no policy: it forwards the entry and the exit of the pointer and nothing else (§5.2-01). The decision of what
/// to do with a region belongs to <see cref="DockInteractionController"/> in Core, which is what makes E3-CA1..E3-CA4
/// verifiable without a window.
/// </para>
/// <para>
/// The events are never marked as handled, so a child element keeps receiving its own pointer input (E5).
/// </para>
/// </remarks>
public static class PointerRegionBehavior
{
    /// <summary>
    /// Attached property that holds the region the element represents.
    /// </summary>
    public static readonly DependencyProperty RegionProperty = DependencyProperty.RegisterAttached(
        "Region",
        typeof(DockRegion),
        typeof(PointerRegionBehavior),
        new PropertyMetadata(DockRegion.None, OnAffinityChanged));

    /// <summary>
    /// Attached property that holds the command executed when the pointer enters the region.
    /// </summary>
    public static readonly DependencyProperty EnterCommandProperty = DependencyProperty.RegisterAttached(
        "EnterCommand",
        typeof(ICommand),
        typeof(PointerRegionBehavior),
        new PropertyMetadata(null, OnAffinityChanged));

    /// <summary>
    /// Attached property that holds the command executed when the pointer leaves the region.
    /// </summary>
    public static readonly DependencyProperty LeaveCommandProperty = DependencyProperty.RegisterAttached(
        "LeaveCommand",
        typeof(ICommand),
        typeof(PointerRegionBehavior),
        new PropertyMetadata(null, OnAffinityChanged));

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
    /// <param name="value">Region the element represents.</param>
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
        if (element is not UIElement target)
        {
            return;
        }

        // Subscribed and unsubscribed every time so that the handler is attached once and only while it is useful.
        target.MouseEnter -= OnMouseEnter;
        target.MouseLeave -= OnMouseLeave;
        target.DragEnter -= OnDragEnter;
        target.DragLeave -= OnDragLeave;
        target.Drop -= OnDrop;
        target.IsVisibleChanged -= OnIsVisibleChanged;

        if (GetRegion(target) == DockRegion.None)
        {
            return;
        }

        var hasEnter = GetEnterCommand(target) is not null;
        var hasLeave = GetLeaveCommand(target) is not null;

        if (hasEnter)
        {
            target.MouseEnter += OnMouseEnter;
            target.DragEnter += OnDragEnter;
        }

        if (hasLeave)
        {
            target.MouseLeave += OnMouseLeave;
            target.DragLeave += OnDragLeave;
            target.Drop += OnDrop;
        }

        if (hasEnter || hasLeave)
        {
            target.IsVisibleChanged += OnIsVisibleChanged;
        }
    }

    private static void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is DependencyObject element)
        {
            Execute(GetEnterCommand(element), GetRegion(element));
        }
    }

    private static void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is DependencyObject element)
        {
            Execute(GetLeaveCommand(element), GetRegion(element));
        }
    }

    private static void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && sender is DependencyObject element)
        {
            Execute(GetEnterCommand(element), GetRegion(element));
        }
    }

    private static void OnDragLeave(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && sender is DependencyObject element)
        {
            Execute(GetLeaveCommand(element), GetRegion(element));
        }
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not UIElement target)
        {
            return;
        }

        // After an external OLE DoDragDrop, WPF may not raise MouseLeave or DragLeave if the pointer exits quickly.
        target.Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            () =>
            {
                if (!target.IsMouseOver)
                {
                    Execute(GetLeaveCommand(target), GetRegion(target));
                }
            });
    }

    private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not UIElement target)
        {
            return;
        }

        if (e.NewValue is false)
        {
            // WPF does not raise MouseLeave when an element collapses (e.g., TabBorder in Expanded or RibbonPanel closing).
            Execute(GetLeaveCommand(target), GetRegion(target));
        }
        else if (e.NewValue is true && target.IsMouseOver)
        {
            // WPF may not raise MouseEnter when an element becomes Visible directly under a stationary pointer.
            Execute(GetEnterCommand(target), GetRegion(target));
        }
    }

    private static void Execute(ICommand? command, DockRegion region)
    {
        if (command is null || !command.CanExecute(region))
        {
            return;
        }

        command.Execute(region);
    }
}
