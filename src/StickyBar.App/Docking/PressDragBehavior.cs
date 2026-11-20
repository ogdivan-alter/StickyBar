using System.Windows;
using System.Windows.Input;

namespace StickyBar.App.Docking;

/// <summary>
/// Executes the bound command when the pointer is pressed on the element that carries it.
/// </summary>
/// <remarks>
/// <para>
/// Contingency of ADR-002. A <see cref="MouseBinding"/> with <c>MouseAction.LeftClick</c> fires on the release, and a
/// native move loop started once the button is up ends immediately, so the drag has to be hooked on the press. This
/// class is the adapter that turns that press into the command of the view model.
/// </para>
/// <para>
/// It contains no geometry and no Win32 code: the movement lives in <c>StickyBar.Interop</c> and the decision in Core.
/// The event is marked as handled so that the release does not raise a second gesture.
/// </para>
/// </remarks>
public static class PressDragBehavior
{
    /// <summary>
    /// Attached property that holds the command executed on the press.
    /// </summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command",
        typeof(ICommand),
        typeof(PressDragBehavior),
        new PropertyMetadata(null, OnCommandChanged));

    /// <summary>
    /// Reads the command attached to an element.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <returns>The attached command, or <see langword="null"/> when it has none.</returns>
    public static ICommand? GetCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (ICommand?)element.GetValue(CommandProperty);
    }

    /// <summary>
    /// Attaches the command executed on the press to an element.
    /// </summary>
    /// <param name="element">Element that carries the property.</param>
    /// <param name="value">Command to execute, or <see langword="null"/> to detach it.</param>
    public static void SetCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.SetValue(CommandProperty, value);
    }

    private static void OnCommandChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not UIElement target)
        {
            return;
        }

        target.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;

        if (e.NewValue is ICommand)
        {
            target.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DependencyObject element)
        {
            return;
        }

        var command = GetCommand(element);

        if (command is null || !command.CanExecute(null))
        {
            return;
        }

        e.Handled = true;
        command.Execute(null);
    }
}
