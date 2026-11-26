using AdminPanel.BL;
using AdminPanel.ViewModels.Basic;

namespace AdminPanel.ViewModels;

// ЗАВДАННЯ i (автентифікація) + ЗАВДАННЯ v (MVVM): ViewModel вікна входу.
// ЗАВДАННЯ iv (DI): сервіси приходять через конструктор
public class LoginViewModel(IAuthService authService, IUserSession userSession) : ValidatableViewModelBase
{
    public const string WrongCredentialsMessage = "Невірний логін або пароль.";

    private string _login = string.Empty;
    private string? _errorMessage;

    // Прив'язана до TextBox; при кожній зміні — перевірка (ЗАВДАННЯ vi)
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

    public bool IsAdmin => userSession.IsAdmin;

    // Пароль приходить з PasswordBox у code-behind і ніде не зберігається
    public bool SignIn(string password)
    {
        // ЗАВДАННЯ vi: без логіна навіть не звертаємося до BL
        if (!ValidateAll())
        {
            return false;
        }

        if (password.Length == 0)
        {
            ErrorMessage = "Введіть пароль.";
            return false;
        }

        try
        {
            if (!authService.Login(Login, password))
            {
                // Однакове повідомлення для неіснуючого логіна й неправильного пароля
                ErrorMessage = WrongCredentialsMessage;
                return false;
            }
        }
        catch (BusinessRuleException ex)
        {
            // ЗАВДАННЯ ii: акаунт не активовано або заблоковано — доступу немає
            ErrorMessage = ex.Message;
            return false;
        }

        ErrorMessage = null;
        return true;
    }

    protected override void ValidateAllProperties() => ValidateLogin();

    private void ValidateLogin()
    {
        SetErrors(nameof(Login), string.IsNullOrWhiteSpace(Login) ? ["Введіть логін."] : []);
    }
}