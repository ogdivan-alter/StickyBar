using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StickyBar.App.Input;

/// <summary>
/// Forwards <see cref="ContextMenu.Opened"/> and <see cref="ContextMenu.Closed"/> events to view-model commands so the
/// dock bar stays open while a context menu popup is active.
/// </summary>
public static class ContextMenuHoldBehavior
{
    /// <summary>
    /// Attached property that holds the command executed when the <see cref="ContextMenu"/> opens.
    /// </summary>
    public static readonly DependencyProperty OpenedCommandProperty = DependencyProperty.RegisterAttached(
        "OpenedCommand",
        typeof(ICommand),
        typeof(ContextMenuHoldBehavior),
        new PropertyMetadata(null, OnOpenedCommandChanged));

    /// <summary>
    /// Attached property that holds the command executed when the <see cref="ContextMenu"/> closes.
    /// </summary>
    public static readonly DependencyProperty ClosedCommandProperty = DependencyProperty.RegisterAttached(
        "ClosedCommand",
        typeof(ICommand),
        typeof(ContextMenuHoldBehavior),
        new PropertyMetadata(null, OnClosedCommandChanged));

    /// <summary>
    /// Reads the command executed when the context menu opens.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <returns>The attached command, or <see langword="null"/> when none is set.</returns>
    public static ICommand? GetOpenedCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (ICommand?)element.GetValue(OpenedCommandProperty);
    }

    /// <summary>
    /// Attaches the command executed when the context menu opens.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <param name="value">Command to execute, or <see langword="null"/> to detach.</param>
    public static void SetOpenedCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.SetValue(OpenedCommandProperty, value);
    }

    /// <summary>
    /// Reads the command executed when the context menu closes.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <returns>The attached command, or <see langword="null"/> when none is set.</returns>
    public static ICommand? GetClosedCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (ICommand?)element.GetValue(ClosedCommandProperty);
    }

    /// <summary>
    /// Attaches the command executed when the context menu closes.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <param name="value">Command to execute, or <see langword="null"/> to detach.</param>
    public static void SetClosedCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.SetValue(ClosedCommandProperty, value);
    }

    private static void OnOpenedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ContextMenu menu)
        {
            return;
        }

        if (e.OldValue is not null)
        {
            menu.Opened -= OnContextMenuOpened;
        }

        if (e.NewValue is not null)
        {
            menu.Opened += OnContextMenuOpened;
        }
    }

    private static void OnClosedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ContextMenu menu)
        {
            return;
        }

        if (e.OldValue is not null)
        {
            menu.Closed -= OnContextMenuClosed;
        }

        if (e.NewValue is not null)
        {
            menu.Closed += OnContextMenuClosed;
        }
    }

    private static void OnContextMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu menu && GetOpenedCommand(menu) is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    private static void OnContextMenuClosed(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu menu && GetClosedCommand(menu) is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
