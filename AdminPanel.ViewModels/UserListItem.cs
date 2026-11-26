using AdminPanel.DTO;

namespace AdminPanel.ViewModels;

// Рядок таблиці користувачів: дані UserDto, підготовлені до показу
public class UserListItem(UserDto user, bool isCurrentUser)
{
    public int UserId { get; } = user.UserId;
    public string Login { get; } = user.Login;
    public bool IsAdmin { get; } = user.IsAdmin;
    public UserStatus Status { get; } = user.Status;
    public DateTime CreatedAt { get; } = user.CreatedAt;

    // Чи це той адмін, що зараз увійшов (себе блокувати не можна)
    public bool IsCurrentUser { get; } = isCurrentUser;

    public string RoleText => IsAdmin ? "Адміністратор" : "Користувач";

    public string StatusText => Status switch
    {
        UserStatus.Active => "Активний",
        UserStatus.Blocked => "Заблокований",
        _ => "Очікує активації"
    };

    public string LoginText => IsCurrentUser ? $"{Login} (ви)" : Login;
}

// Пункт списку «Сортувати за»
public record SortOption(string Title, AdminPanel.BL.UserSortField Field);