using System.Collections;
using System.ComponentModel;

namespace AdminPanel.ViewModels.Basic;

// ЗАВДАННЯ vi (валідація): ViewModel з INotifyDataErrorInfo.
// WPF сам підсвічує червоним поле, для якого тут є помилка
public abstract class ValidatableViewModelBase : ViewModelBase, INotifyDataErrorInfo
{
    // Назва властивості → список помилок
    private readonly Dictionary<string, List<string>> _errors = new();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count > 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        return propertyName is not null && _errors.TryGetValue(propertyName, out var errors)
            ? errors
            : Enumerable.Empty<string>();
    }

    // Перевіряє всі поля (наприклад, перед входом). Повертає true, якщо помилок немає
    public bool ValidateAll()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    protected abstract void ValidateAllProperties();

    // Порожній список — помилку прибрано
    protected void SetErrors(string propertyName, IEnumerable<string> errors)
    {
        var list = errors.ToList();
        if (list.Count == 0)
        {
            if (!_errors.Remove(propertyName))
            {
                return;
            }
        }
        else
        {
            _errors[propertyName] = list;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }
}