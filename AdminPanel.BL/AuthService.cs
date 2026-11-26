using System.Text.RegularExpressions;
using AdminPanel.DAL.Interfaces;
using AdminPanel.DTO;

namespace AdminPanel.BL;

public interface IAuthService
{
    // true — вхід виконано; false — невірний логін або пароль.
    // BusinessRuleException — пароль правильний, але акаунт не активовано або заблоковано
    bool Login(string login, string password);

    void Logout();

    // Реєстрація: новий акаунт отримує стан «Очікує активації»
    int Register(string login, string password);
}

// ЗАВДАННЯ i (автентифікація): вхід, вихід, реєстрація.
// ЗАВДАННЯ iv (DI): усі залежності приходять через конструктор
public partial class AuthService(IUserDal userDal, IPasswordHasher passwordHasher, IUserSession userSession) : IAuthService
{
    // ЗАВДАННЯ vi (валідація): межі для логіна й пароля
    public const int LoginMinLength = 3;
    public const int LoginMaxLength = 50;
    public const int PasswordMinLength = 6;

    public const string NotActivatedMessage = "Обліковий запис ще не активовано адміністратором.";
    public const string BlockedMessage = "Обліковий запис заблоковано. Зверніться до адміністратора.";

    // Логін: лише латинські літери, цифри, «_» і «.»
    [GeneratedRegex(@"^[A-Za-z0-9_.]+$")]
    private static partial Regex LoginRegex();

    public bool Login(string login, string password)
    {
        var user = userDal.GetByLogin(login.Trim());
        if (user is null)
        {
            // Рахуємо хеш і для неіснуючого логіна, щоб час відповіді не підказував, чи існує користувач
            passwordHasher.Hash(password, Guid.Empty);
            return false;
        }

        if (!passwordHasher.Verify(password, user.PasswordSalt, user.PasswordHash))
        {
            return false;
        }

        // ЗАВДАННЯ ii: неактивований або заблокований користувач не отримує доступу.
        // Стан перевіряємо лише після правильного пароля
        if (user.Status == UserStatus.Pending)
        {
            throw new BusinessRuleException(NotActivatedMessage);
        }

        if (user.Status == UserStatus.Blocked)
        {
            throw new BusinessRuleException(BlockedMessage);
        }

        userSession.SignIn(user);
        return true;
    }

    // Роль: «може розлогуватися»
    public void Logout()
    {
        userSession.SignOut();
    }

    public int Register(string login, string password)
    {
        var normalizedLogin = login.Trim();
        var errors = new List<string>();

        // ЗАВДАННЯ vi (валідація) на рівні BL: збираємо всі помилки одразу
        if (ValidateLogin(normalizedLogin) is { } loginError)
        {
            errors.Add(loginError);
        }

        if (password.Length < PasswordMinLength)
        {
            errors.Add($"Пароль має містити щонайменше {PasswordMinLength} символів.");
        }

        if (errors.Count == 0 && userDal.GetByLogin(normalizedLogin) is not null)
        {
            errors.Add($"Логін \"{normalizedLogin}\" уже зайнятий.");
        }

        if (errors.Count > 0)
        {
            throw new BusinessRuleException(errors);
        }

        // Новий користувач — не адмін і чекає активації адміністратором
        var salt = Guid.NewGuid();
        return userDal.Create(new UserDto
        {
            Login = normalizedLogin,
            PasswordSalt = salt,
            PasswordHash = passwordHasher.Hash(password, salt),
            IsAdmin = false,
            Status = UserStatus.Pending
        });
    }

    // ЗАВДАННЯ vi: правило для логіна. Повертає текст помилки або null.
    // Ним користується й RegisterViewModel, щоб правило було в одному місці
    public static string? ValidateLogin(string? login)
    {
        var value = (login ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return "Введіть логін.";
        }

        if (value.Length < LoginMinLength || value.Length > LoginMaxLength)
        {
            return $"Логін має містити від {LoginMinLength} до {LoginMaxLength} символів.";
        }

        if (!LoginRegex().IsMatch(value))
        {
            return "Логін: лише латинські літери, цифри, «_» і «.».";
        }

        return null;
    }
}