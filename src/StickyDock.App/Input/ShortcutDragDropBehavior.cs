using System.Windows;
using System.Windows.Input;
using StickyDock.Core.Shortcuts;
using StickyDock.Core.ViewModels;

namespace StickyDock.App.Input;

/// <summary>
/// Attached behavior that translates WPF drag &amp; drop gestures (external file drops and internal shortcut reordering)
/// into view model commands without placing logic in code-behind (E5-CA1, E5-CA3, E5-CA5, §5.2-01).
/// </summary>
public static class ShortcutDragDropBehavior
{
    private const string InternalShortcutDataFormat = "StickyDock.InternalShortcutItem";

    private static readonly DependencyProperty DragStartPointProperty = DependencyProperty.RegisterAttached(
        "DragStartPoint",
        typeof(Point?),
        typeof(ShortcutDragDropBehavior),
        new PropertyMetadata(null));

    /// <summary>
    /// Attached property holding the command executed when external files are dropped onto an element.
    /// </summary>
    public static readonly DependencyProperty ExternalDropCommandProperty = DependencyProperty.RegisterAttached(
        "ExternalDropCommand",
        typeof(ICommand),
        typeof(ShortcutDragDropBehavior),
        new PropertyMetadata(null, OnExternalDropCommandChanged));

    /// <summary>
    /// Attached property holding the <see cref="ShortcutItemViewModel"/> represented by a draggable shortcut button.
    /// </summary>
    public static readonly DependencyProperty DragSourceItemProperty = DependencyProperty.RegisterAttached(
        "DragSourceItem",
        typeof(ShortcutItemViewModel),
        typeof(ShortcutDragDropBehavior),
        new PropertyMetadata(null, OnInternalDragChanged));

    /// <summary>
    /// Attached property holding the command executed when a shortcut is dropped onto another shortcut to reorder.
    /// </summary>
    public static readonly DependencyProperty ReorderCommandProperty = DependencyProperty.RegisterAttached(
        "ReorderCommand",
        typeof(ICommand),
        typeof(ShortcutDragDropBehavior),
        new PropertyMetadata(null, OnInternalDragChanged));

    /// <summary>
    /// Attached property holding the command executed just before an internal shortcut drag starts.
    /// </summary>
    public static readonly DependencyProperty DragStartedCommandProperty = DependencyProperty.RegisterAttached(
        "DragStartedCommand",
        typeof(ICommand),
        typeof(ShortcutDragDropBehavior),
        new PropertyMetadata(null));

    /// <summary>
    /// Attached property holding the command executed after an internal shortcut drag ends.
    /// </summary>
    public static readonly DependencyProperty DragEndedCommandProperty = DependencyProperty.RegisterAttached(
        "DragEndedCommand",
        typeof(ICommand),
        typeof(ShortcutDragDropBehavior),
        new PropertyMetadata(null));

    /// <summary>
    /// Attached property holding the target <see cref="RibbonViewModel"/> for dropping an internal shortcut onto a ribbon.
    /// </summary>
    public static readonly DependencyProperty TargetRibbonProperty = DependencyProperty.RegisterAttached(
        "TargetRibbon",
        typeof(RibbonViewModel),
        typeof(ShortcutDragDropBehavior),
        new PropertyMetadata(null, OnRibbonDropTargetChanged));

    /// <summary>
    /// Attached property holding the command executed when an internal shortcut is dropped onto <see cref="TargetRibbonProperty"/>.
    /// </summary>
    public static readonly DependencyProperty MoveToRibbonCommandProperty = DependencyProperty.RegisterAttached(
        "MoveToRibbonCommand",
        typeof(ICommand),
        typeof(ShortcutDragDropBehavior),
        new PropertyMetadata(null, OnRibbonDropTargetChanged));

    /// <summary>Gets the external drop command attached to <paramref name="element"/>.</summary>
    public static ICommand? GetExternalDropCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (ICommand?)element.GetValue(ExternalDropCommandProperty);
    }

    /// <summary>Sets the external drop command attached to <paramref name="element"/>.</summary>
    public static void SetExternalDropCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ExternalDropCommandProperty, value);
    }

    /// <summary>Gets the drag source shortcut item attached to <paramref name="element"/>.</summary>
    public static ShortcutItemViewModel? GetDragSourceItem(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (ShortcutItemViewModel?)element.GetValue(DragSourceItemProperty);
    }

    /// <summary>Sets the drag source shortcut item attached to <paramref name="element"/>.</summary>
    public static void SetDragSourceItem(DependencyObject element, ShortcutItemViewModel? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(DragSourceItemProperty, value);
    }

    /// <summary>Gets the internal reorder command attached to <paramref name="element"/>.</summary>
    public static ICommand? GetReorderCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (ICommand?)element.GetValue(ReorderCommandProperty);
    }

    /// <summary>Sets the internal reorder command attached to <paramref name="element"/>.</summary>
    public static void SetReorderCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ReorderCommandProperty, value);
    }

    /// <summary>Gets the drag-started command attached to <paramref name="element"/>.</summary>
    public static ICommand? GetDragStartedCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (ICommand?)element.GetValue(DragStartedCommandProperty);
    }

    /// <summary>Sets the drag-started command attached to <paramref name="element"/>.</summary>
    public static void SetDragStartedCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(DragStartedCommandProperty, value);
    }

    /// <summary>Gets the drag-ended command attached to <paramref name="element"/>.</summary>
    public static ICommand? GetDragEndedCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (ICommand?)element.GetValue(DragEndedCommandProperty);
    }

    /// <summary>Sets the drag-ended command attached to <paramref name="element"/>.</summary>
    public static void SetDragEndedCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(DragEndedCommandProperty, value);
    }

    /// <summary>Gets the target ribbon attached to <paramref name="element"/>.</summary>
    public static RibbonViewModel? GetTargetRibbon(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (RibbonViewModel?)element.GetValue(TargetRibbonProperty);
    }

    /// <summary>Sets the target ribbon attached to <paramref name="element"/>.</summary>
    public static void SetTargetRibbon(DependencyObject element, RibbonViewModel? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(TargetRibbonProperty, value);
    }

    /// <summary>Gets the move-to-ribbon command attached to <paramref name="element"/>.</summary>
    public static ICommand? GetMoveToRibbonCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (ICommand?)element.GetValue(MoveToRibbonCommandProperty);
    }

    /// <summary>Sets the move-to-ribbon command attached to <paramref name="element"/>.</summary>
    public static void SetMoveToRibbonCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(MoveToRibbonCommandProperty, value);
    }

    private static void OnExternalDropCommandChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not UIElement target)
        {
            return;
        }

        target.DragOver -= OnExternalDragOver;
        target.Drop -= OnExternalDrop;

        if (e.NewValue is not ICommand)
        {
            return;
        }

        target.AllowDrop = true;
        target.DragOver += OnExternalDragOver;
        target.Drop += OnExternalDrop;
    }

    private static void OnInternalDragChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not UIElement target)
        {
            return;
        }

        target.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
        target.PreviewMouseMove -= OnPreviewMouseMove;
        target.DragOver -= OnInternalDragOver;
        target.Drop -= OnInternalDrop;

        if (GetDragSourceItem(target) is null)
        {
            return;
        }

        target.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        target.PreviewMouseMove += OnPreviewMouseMove;

        if (GetReorderCommand(target) is not null)
        {
            target.AllowDrop = true;
            target.DragOver += OnInternalDragOver;
            target.Drop += OnInternalDrop;
        }
    }

    private static void OnRibbonDropTargetChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not UIElement target)
        {
            return;
        }

        target.DragOver -= OnRibbonDragOver;
        target.Drop -= OnRibbonDrop;

        if (GetTargetRibbon(target) is null || GetMoveToRibbonCommand(target) is null)
        {
            return;
        }

        target.AllowDrop = true;
        target.DragOver += OnRibbonDragOver;
        target.Drop += OnRibbonDrop;
    }

    private static void OnExternalDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private static void OnExternalDrop(object sender, DragEventArgs e)
    {
        if (sender is not DependencyObject element || !e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
        {
            return;
        }

        var command = GetExternalDropCommand(element);

        if (command is not null && command.CanExecute(files))
        {
            command.Execute(files);
            e.Handled = true;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DependencyObject element)
        {
            element.SetValue(DragStartPointProperty, e.GetPosition(null));
        }
    }

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not DependencyObject element)
        {
            return;
        }

        var startPoint = (Point?)element.GetValue(DragStartPointProperty);

        if (startPoint is null)
        {
            return;
        }

        var current = e.GetPosition(null);

        if (Math.Abs(current.X - startPoint.Value.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - startPoint.Value.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var sourceItem = GetDragSourceItem(element);

        if (sourceItem is null)
        {
            return;
        }

        element.SetValue(DragStartPointProperty, null);
        var data = new DataObject(InternalShortcutDataFormat, sourceItem);
        var dragStarted = GetDragStartedCommand(element);
        var dragEnded = GetDragEndedCommand(element);

        if (dragStarted is not null && dragStarted.CanExecute(null))
        {
            dragStarted.Execute(null);
        }

        try
        {
            DragDrop.DoDragDrop(element, data, DragDropEffects.Move);
        }
        finally
        {
            if (dragEnded is not null && dragEnded.CanExecute(null))
            {
                dragEnded.Execute(null);
            }
        }
    }

    private static void OnInternalDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(InternalShortcutDataFormat) || sender is not DependencyObject element)
        {
            return;
        }

        if (e.Data.GetData(InternalShortcutDataFormat) is not ShortcutItemViewModel source)
        {
            return;
        }

        var target = GetDragSourceItem(element);

        if (target is null
            || string.Equals(source.Id, target.Id, StringComparison.Ordinal)
            || !string.Equals(source.ContainerRibbonId, target.ContainerRibbonId, StringComparison.Ordinal))
        {
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private static void OnInternalDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(InternalShortcutDataFormat) || sender is not DependencyObject element)
        {
            return;
        }

        if (e.Data.GetData(InternalShortcutDataFormat) is not ShortcutItemViewModel source)
        {
            return;
        }

        var target = GetDragSourceItem(element);

        if (target is null
            || string.Equals(source.Id, target.Id, StringComparison.Ordinal)
            || !string.Equals(source.ContainerRibbonId, target.ContainerRibbonId, StringComparison.Ordinal))
        {
            return;
        }

        var command = GetReorderCommand(element);
        var request = new ShortcutReorderRequest(source.Id, target.Id);

        if (command is not null && command.CanExecute(request))
        {
            command.Execute(request);
            e.Handled = true;
        }
    }

    private static void OnRibbonDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(InternalShortcutDataFormat) || sender is not DependencyObject element)
        {
            return;
        }

        if (e.Data.GetData(InternalShortcutDataFormat) is not ShortcutItemViewModel source)
        {
            return;
        }

        var targetRibbon = GetTargetRibbon(element);

        if (targetRibbon is null
            || string.Equals(source.ContainerRibbonId, targetRibbon.Id, StringComparison.Ordinal))
        {
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private static void OnRibbonDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(InternalShortcutDataFormat) || sender is not DependencyObject element)
        {
            return;
        }

        if (e.Data.GetData(InternalShortcutDataFormat) is not ShortcutItemViewModel source)
        {
            return;
        }

        var targetRibbon = GetTargetRibbon(element);

        if (targetRibbon is null
            || string.Equals(source.ContainerRibbonId, targetRibbon.Id, StringComparison.Ordinal))
        {
            return;
        }

        var command = GetMoveToRibbonCommand(element);

        if (command is not null && command.CanExecute(source.Id))
        {
            command.Execute(source.Id);
            e.Handled = true;
        }
    }
}
