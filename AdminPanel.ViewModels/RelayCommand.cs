using System.Windows.Input;

namespace AdminPanel.ViewModels.Basic;

// ЗАВДАННЯ v (команди): реалізація ICommand.
// Execute — виконати дію, CanExecute — чи доступна вона (кнопка стає неактивною).
// Після зміни стану викликайте RaiseCanExecuteChanged, щоб WPF оновив кнопку
public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            execute();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}