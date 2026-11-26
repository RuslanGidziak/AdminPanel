namespace AdminPanel.DTO;

// Один рядок таблиці dbo.tblUsers
public class UserDto
{
    public int UserId { get; set; }
    public string Login { get; set; } = string.Empty;

    // Пароль у базі не зберігається — лише хеш і сіль
    public byte[] PasswordHash { get; set; } = [];
    public Guid PasswordSalt { get; set; }

    // Роль: true — адміністратор, false — звичайний користувач
    public bool IsAdmin { get; set; }

    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}