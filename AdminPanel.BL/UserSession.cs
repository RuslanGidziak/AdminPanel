using AdminPanel.DTO;

namespace AdminPanel.BL;

public interface IUserSession
{
    UserDto? CurrentUser { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    void SignIn(UserDto user);
    void SignOut();
}

// ЗАВДАННЯ i: зберігає, хто зараз увійшов.
// ЗАВДАННЯ iv (DI): реєструється як Singleton — одна сесія на весь застосунок
public class UserSession : IUserSession
{
    public UserDto? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    // ЗАВДАННЯ ii: роль визначає, що користувачу доступно
    public bool IsAdmin => CurrentUser is { IsAdmin: true, Status: UserStatus.Active };

    public void SignIn(UserDto user)
    {
        CurrentUser = user;
    }

    // Роль «може розлогуватися»
    public void SignOut()
    {
        CurrentUser = null;
    }
}