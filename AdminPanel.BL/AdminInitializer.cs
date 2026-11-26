using AdminPanel.DAL.Interfaces;
using AdminPanel.DTO;

namespace AdminPanel.BL;

public interface IAdminInitializer
{
    // Якщо в системі немає активного адміністратора, створює або відновлює акаунт admin.
    // Повертає true, якщо щось змінено
    bool EnsureAdminExists();
}

// Роль: «в системі хоча б один адмін повинен бути».
// Викликається під час кожного запуску програми (App.xaml.cs)
public class AdminInitializer(IUserDal userDal, IPasswordHasher passwordHasher) : IAdminInitializer
{
    public const string DefaultLogin = "admin";
    public const string DefaultPassword = "Admin123!";

    public bool EnsureAdminExists()
    {
        // Активний адмін уже є — нічого не робимо
        if (userDal.GetAll().Any(u => u is { IsAdmin: true, Status: UserStatus.Active }))
        {
            return false;
        }

        // Акаунт admin є, але заблокований — відновлюємо
        var existing = userDal.GetByLogin(DefaultLogin);
        if (existing is not null)
        {
            existing.IsAdmin = true;
            existing.Status = UserStatus.Active;
            userDal.Update(existing);
            return true;
        }

        // База порожня — створюємо admin / Admin123!
        var salt = Guid.NewGuid();
        userDal.Create(new UserDto
        {
            Login = DefaultLogin,
            PasswordSalt = salt,
            PasswordHash = passwordHasher.Hash(DefaultPassword, salt),
            IsAdmin = true,
            Status = UserStatus.Active
        });
        return true;
    }
}