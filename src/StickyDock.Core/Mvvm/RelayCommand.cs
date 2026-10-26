using System.Windows.Input;

namespace StickyDock.Core.Mvvm;

/// <summary>
/// <see cref="ICommand"/> implementation that delegates execution and availability to callbacks.
/// </summary>
/// <remarks>
/// WPF does not automatically re-query <see cref="CanExecute"/> unless <c>CommandManager</c> is involved,
/// which lives in the presentation framework and not in this assembly. Owners of the command must therefore call
/// <see cref="RaiseCanExecuteChanged"/> when the underlying state changes.
/// </remarks>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">Action performed when the command is executed.</param>
    /// <param name="canExecute">Optional predicate that decides whether the command can run.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// Notifies bound controls that <see cref="CanExecute"/> must be evaluated again.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
