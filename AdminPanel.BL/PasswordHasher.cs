using System.Security.Cryptography;

namespace AdminPanel.BL;

// ЗАВДАННЯ iv (DI): інтерфейс, щоб у тестах AuthService підставити мок
public interface IPasswordHasher
{
    byte[] Hash(string password, Guid salt);
    bool Verify(string password, Guid salt, byte[] expectedHash);
}

// ЗАВДАННЯ i (автентифікація): паролі зберігаються лише як хеш.
// PBKDF2 + HMAC-SHA512, 210 000 ітерацій — рекомендація OWASP
public class PasswordHasher : IPasswordHasher
{
    public const int Iterations = 210_000;
    public const int HashSize = 32;

    public byte[] Hash(string password, Guid salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt.ToByteArray(), Iterations, HashAlgorithmName.SHA512, HashSize);
    }

    public bool Verify(string password, Guid salt, byte[] expectedHash)
    {
        // Порівняння за сталий час: час відповіді не підказує, скільки байтів збіглося
        return CryptographicOperations.FixedTimeEquals(Hash(password, salt), expectedHash);
    }
}