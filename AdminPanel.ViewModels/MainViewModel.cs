using AdminPanel.BL;
using AdminPanel.ViewModels.Basic;

namespace AdminPanel.ViewModels;

// Роль: «попадає на стартову сторінку». ViewModel головного вікна
public class MainViewModel(IUserSession userSession) : ViewModelBase
{
    public string UserName => userSession.CurrentUser?.Login ?? string.Empty;

    // ЗАВДАННЯ ii: від цього залежить, чи видно пункт меню «Адміністрування»
    public bool IsAdmin => userSession.IsAdmin;

    public string RoleText => IsAdmin ? "Адміністратор" : "Користувач";

    public string Greeting => $"Вітаємо, {UserName}!";
}