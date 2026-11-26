using AdminPanel.DAL.Interfaces;
using AdminPanel.DTO;

namespace AdminPanel.BL;

// За яким полем сортувати таблицю користувачів
public enum UserSortField
{
    Id,
    Login,
    Role,
    Status,
    CreatedAt
}

public interface IUserService
{
    // Пошук за частиною логіна (без урахування регістру) і сортування. Лише для адміністратора
    List<UserDto> Search(string? text, UserSortField sortBy = UserSortField.Id, bool descending = false);

    void Activate(int userId);

    void Block(int userId);
}

// ЗАВДАННЯ i: функції ролі Administrator — таблиця користувачів, пошук, сортування,
// активація та блокування акаунтів.
// ЗАВДАННЯ iv (DI): залежності приходять через конструктор
public class UserService(IUserDal userDal, IUserSession userSession) : IUserService
{
    public const string NotAdminMessage = "Ця дія доступна лише адміністратору.";
    public const string SelfBlockMessage = "Не можна заблокувати власний обліковий запис.";
    public const string LastAdminMessage = "У системі має залишитися хоча б один активний адміністратор.";

    // Роль: «робити пошук, сортувати»
    public List<UserDto> Search(string? text, UserSortField sortBy = UserSortField.Id, bool descending = false)
    {
        RequireAdmin();

        IEnumerable<UserDto> users = userDal.GetAll();
        if (!string.IsNullOrWhiteSpace(text))
        {
            var part = text.Trim();
            users = users.Where(u => u.Login.Contains(part, StringComparison.CurrentCultureIgnoreCase));
        }

        return Sort(users, sortBy, descending).ToList();
    }

    // Роль: «активувати accounts користувачів»
    public void Activate(int userId)
    {
        RequireAdmin();
        var user = GetUserOrThrow(userId);

        if (user.Status == UserStatus.Active)
        {
            throw new BusinessRuleException($"Обліковий запис \"{user.Login}\" уже активний.");
        }

        user.Status = UserStatus.Active;
        SaveOrThrow(user);
    }

    // Роль: «блокувати accounts користувачів»
    public void Block(int userId)
    {
        var admin = RequireAdmin();

        // Роль: «в системі хоча б один адмін повинен бути».
        // Адмін, що виконує дію, завжди активний. Якщо він не може заблокувати себе,
        // то після будь-якого блокування в системі лишається щонайменше він сам
        if (userId == admin.UserId)
        {
            throw new BusinessRuleException(SelfBlockMessage);
        }

        var user = GetUserOrThrow(userId);
        if (user.Status == UserStatus.Blocked)
        {
            throw new BusinessRuleException($"Обліковий запис \"{user.Login}\" уже заблоковано.");
        }

        // Додатковий захист правила «хоча б один адмін»
        if (user is { IsAdmin: true, Status: UserStatus.Active } &&
            !userDal.GetAll().Any(u => u.UserId != user.UserId && u is { IsAdmin: true, Status: UserStatus.Active }))
        {
            throw new BusinessRuleException(LastAdminMessage);
        }

        user.Status = UserStatus.Blocked;
        SaveOrThrow(user);
    }

    public static IEnumerable<UserDto> Sort(IEnumerable<UserDto> users, UserSortField sortBy, bool descending)
    {
        var ordered = sortBy switch
        {
            UserSortField.Login => Order(users, u => u.Login, descending, StringComparer.CurrentCultureIgnoreCase),
            UserSortField.Role => Order(users, u => u.IsAdmin, descending),
            UserSortField.Status => Order(users, u => u.Status, descending),
            UserSortField.CreatedAt => Order(users, u => u.CreatedAt, descending),
            _ => Order(users, u => u.UserId, descending)
        };

        // Однакові значення — за Id, щоб порядок був стабільним
        return ordered.ThenBy(u => u.UserId);
    }

    private static IOrderedEnumerable<UserDto> Order<TKey>(IEnumerable<UserDto> users, Func<UserDto, TKey> key,
        bool descending, IComparer<TKey>? comparer = null)
    {
        return descending ? users.OrderByDescending(key, comparer) : users.OrderBy(key, comparer);
    }

    // ЗАВДАННЯ ii: права перевіряються в BL, а не лише в UI.
    // Користувача перечитуємо з бази: адміністратора могли заблокувати, поки він працював
    private UserDto RequireAdmin()
    {
        var current = userSession.CurrentUser ?? throw new BusinessRuleException("Спочатку увійдіть у систему.");
        var fresh = userDal.GetById(current.UserId);
        if (fresh is not { IsAdmin: true, Status: UserStatus.Active })
        {
            throw new BusinessRuleException(NotAdminMessage);
        }

        return fresh;
    }

    private UserDto GetUserOrThrow(int userId)
    {
        return userDal.GetById(userId) ?? throw new BusinessRuleException("Користувача не знайдено. Можливо, його вже видалили.");
    }

    private void SaveOrThrow(UserDto user)
    {
        if (!userDal.Update(user))
        {
            throw new BusinessRuleException("Користувача не знайдено. Можливо, його вже видалили.");
        }
    }
}