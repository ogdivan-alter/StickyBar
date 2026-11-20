using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StickyBar.Core.Mvvm;

/// <summary>
/// Base implementation of <see cref="INotifyPropertyChanged"/> for observable state holders.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Assigns <paramref name="value"/> to <paramref name="field"/> and raises <see cref="PropertyChanged"/>
    /// only when the value actually changes.
    /// </summary>
    /// <typeparam name="TValue">Type of the observed value.</typeparam>
    /// <param name="field">Backing field of the property.</param>
    /// <param name="value">New value.</param>
    /// <param name="propertyName">Name of the property; supplied by the compiler.</param>
    /// <returns><see langword="true"/> when the value changed and the notification was raised.</returns>
    protected bool SetProperty<TValue>(ref TValue field, TValue value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<TValue>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for the given property.
    /// </summary>
    /// <param name="propertyName">Name of the property; supplied by the compiler.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
