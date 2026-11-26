using AdminPanel.BL;
using AdminPanel.ViewModels.Basic;

namespace AdminPanel.ViewModels;

// ЗАВДАННЯ v (MVVM): ViewModel вікна реєстрації
public class RegisterViewModel(IAuthService authService) : ValidatableViewModelBase
{
    private string _login = string.Empty;
    private string? _errorMessage;

    public string Login
    {
        get => _login;
        set
        {
            if (SetProperty(ref _login, value))
            {
                ValidateLogin();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    // Повертає true, якщо акаунт створено і вікно можна закривати
    public bool Register(string password, string confirmPassword)
    {
        // ЗАВДАННЯ vi (валідація): логін, довжина пароля, збіг паролів
        if (!ValidateAll())
        {
            return false;
        }

        // Межа береться з BL, щоб числа не розходилися
        if (password.Length < AuthService.PasswordMinLength)
        {
            ErrorMessage = $"Пароль має містити щонайменше {AuthService.PasswordMinLength} символів.";
            return false;
        }

        if (password != confirmPassword)
        {
            ErrorMessage = "Паролі не збігаються.";
            return false;
        }

        try
        {
            authService.Register(Login, password);
            ErrorMessage = null;
            return true;
        }
        catch (BusinessRuleException ex)
        {
            // Наприклад: «Логін уже зайнятий»
            ErrorMessage = ex.Message;
            return false;
        }
    }

    protected override void ValidateAllProperties() => ValidateLogin();

    // Те саме правило, що й у BL (AuthService.ValidateLogin)
    private void ValidateLogin()
    {
        var error = AuthService.ValidateLogin(Login);
        SetErrors(nameof(Login), error is null ? [] : [error]);
    }
}