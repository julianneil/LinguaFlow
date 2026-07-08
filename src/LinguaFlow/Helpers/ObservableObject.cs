namespace LinguaFlow.Helpers;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Base implementation for view models that notify WPF when bound values change.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Updates a backing field and raises change notifications when the value changes.
    /// </summary>
    /// <typeparam name="T">The property value type.</typeparam>
    /// <param name="field">The backing field to update.</param>
    /// <param name="value">The new value requested by the caller.</param>
    /// <param name="propertyName">The property name supplied by the compiler.</param>
    /// <returns><see langword="true" /> when the value changed; otherwise <see langword="false" />.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged" /> event for a bound property.
    /// </summary>
    /// <param name="propertyName">The property name supplied by the compiler.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
