using System.Windows.Input;

namespace StickyBar.Core.Mvvm;

/// <summary>
/// <see cref="ICommand"/> implementation that delegates execution and availability to callbacks that receive the
/// parameter of the command.
/// </summary>
/// <typeparam name="TParameter">Type of the parameter the command expects.</typeparam>
/// <remarks>
/// Used by the region commands of the bar, whose parameter is the <c>DockRegion</c> of the element that raises it. The
/// command is unavailable for any other parameter, so a missing or wrong parameter cannot reach the policy.
/// </remarks>
public sealed class RelayCommand<TParameter> : ICommand
{
    private readonly Action<TParameter> _execute;
    private readonly Func<TParameter, bool>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand{TParameter}"/> class.
    /// </summary>
    /// <param name="execute">Action performed when the command is executed.</param>
    /// <param name="canExecute">Optional predicate that decides whether the command can run.</param>
    public RelayCommand(Action<TParameter> execute, Func<TParameter, bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
        => parameter is TParameter typed && (_canExecute?.Invoke(typed) ?? true);

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (parameter is TParameter typed)
        {
            _execute(typed);
        }
    }

    /// <summary>
    /// Notifies bound controls that <see cref="CanExecute"/> must be evaluated again.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
