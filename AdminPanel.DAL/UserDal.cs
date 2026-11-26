using System.Data;
using AdminPanel.DAL.Interfaces;
using AdminPanel.DTO;
using Microsoft.Data.SqlClient;

namespace AdminPanel.DAL;

// DAL — шар доступу до даних: робота з таблицею dbo.tblUsers через ADO.NET.
// Усі значення передаються параметрами (@Login тощо), тому SQL-ін'єкція неможлива
public class UserDal(string connectionString) : IUserDal
{
    private const string SelectSql =
        "SELECT UserId, Login, PasswordHash, PasswordSalt, IsAdmin, Status, CreatedAt FROM dbo.tblUsers";

    // Усі користувачі — для таблиці на сторінці адміністратора
    public List<UserDto> GetAll()
    {
        var users = new List<UserDto>();

        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(SelectSql + " ORDER BY UserId", connection);
        connection.Open();

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            users.Add(ReadUser(reader));
        }

        return users;
    }

    public UserDto? GetById(int userId)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(SelectSql + " WHERE UserId = @UserId", connection);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        connection.Open();

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadUser(reader) : null;
    }

    // Пошук за логіном — для входу в систему
    public UserDto? GetByLogin(string login)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(SelectSql + " WHERE Login = @Login", connection);
        command.Parameters.Add("@Login", SqlDbType.NVarChar, 50).Value = login;
        connection.Open();

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadUser(reader) : null;
    }

    // Реєстрація: OUTPUT INSERTED.UserId повертає Id нового рядка
    public int Create(UserDto user)
    {
        const string sql =
            "INSERT INTO dbo.tblUsers (Login, PasswordHash, PasswordSalt, IsAdmin, Status) " +
            "OUTPUT INSERTED.UserId " +
            "VALUES (@Login, @PasswordHash, @PasswordSalt, @IsAdmin, @Status)";

        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(sql, connection);
        AddParameters(command, user);
        connection.Open();

        return (int)command.ExecuteScalar()!;
    }

    // Збереження змін (активація / блокування)
    public bool Update(UserDto user)
    {
        const string sql =
            "UPDATE dbo.tblUsers SET Login = @Login, PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt, " +
            "IsAdmin = @IsAdmin, Status = @Status WHERE UserId = @UserId";

        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand(sql, connection);
        AddParameters(command, user);
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
        connection.Open();

        return command.ExecuteNonQuery() > 0;
    }

    private static void AddParameters(SqlCommand command, UserDto user)
    {
        command.Parameters.Add("@Login", SqlDbType.NVarChar, 50).Value = user.Login;
        command.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 64).Value = user.PasswordHash;
        command.Parameters.Add("@PasswordSalt", SqlDbType.UniqueIdentifier).Value = user.PasswordSalt;
        command.Parameters.Add("@IsAdmin", SqlDbType.Bit).Value = user.IsAdmin;
        command.Parameters.Add("@Status", SqlDbType.TinyInt).Value = (byte)user.Status;
    }

    // Рядок результату SELECT → UserDto (порядок стовпців як у SelectSql)
    private static UserDto ReadUser(SqlDataReader reader) => new()
    {
        UserId = reader.GetInt32(0),
        Login = reader.GetString(1),
        PasswordHash = (byte[])reader[2],
        PasswordSalt = reader.GetGuid(3),
        IsAdmin = reader.GetBoolean(4),
        Status = (UserStatus)reader.GetByte(5),
        CreatedAt = reader.GetDateTime(6)
    };
}