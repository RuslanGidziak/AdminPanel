using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdminPanel.ViewModels.Basic;

// ЗАВДАННЯ v (MVVM): базовий клас для всіх ViewModel.
// INotifyPropertyChanged повідомляє View, що значення змінилося, і прив'язка оновлює екран
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Записує значення і повідомляє View лише тоді, коли воно справді змінилося
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
}