namespace LinguaFlow.Helpers;

using System.Windows.Input;

/// <summary>
/// Lightweight command implementation for binding WPF controls to view model actions.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> execute;
    private readonly Predicate<object?>? canExecute;

    /// <summary>
    /// Creates a command with an execute action and optional availability check.
    /// </summary>
    /// <param name="execute">The action to run when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate used to enable or disable the command.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="execute" /> is missing.</exception>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return canExecute?.Invoke(parameter) ?? true;
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        execute(parameter);
    }

    /// <summary>
    /// Notifies WPF that command availability may have changed.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
