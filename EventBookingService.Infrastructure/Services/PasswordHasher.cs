using System.Security.Cryptography;
using System.Text;

using EventBookingService.Application.Interfaces;

namespace EventBookingService.Infrastructure.Services;

/// <summary>
/// Компонент для хеширования паролей и проверки соответствия
/// </summary>
public class PasswordHasher :IPasswordHasher
{
    /// <summary>
    /// Хеширует пароль с использованием SHA-256
    /// </summary>
    /// <param name="password">Исходный пароль</param>
    /// <returns>Хеш пароля в виде строки Base64</returns>
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Пароль не может быть пустым", nameof(password));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Проверяет соответствие пароля хешу
    /// </summary>
    /// <param name="password">Пароль для проверки</param>
    /// <param name="storedHash">Сохраненный хеш</param>
    /// <returns>True, если пароль соответствует хешу</returns>
    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Пароль не может быть пустым", nameof(password));

        if (string.IsNullOrEmpty(storedHash))
            throw new ArgumentException("Хеш не может быть пустым", nameof(storedHash));

        // Вычисляем хеш для проверяемого пароля
        string computedHash = HashPassword(password);

        // Сравниваем хеши
        return CompareHashes(computedHash, storedHash);
    }

    /// <summary>
    /// Безопасное сравнение хешей
    /// </summary>
    private bool CompareHashes(string hash1, string hash2)
    {
        byte[] bytes1 = Encoding.UTF8.GetBytes(hash1);
        byte[] bytes2 = Encoding.UTF8.GetBytes(hash2);

        // CryptographicOperations.FixedTimeEquals встроен в .NET 
        // Он сравнивает массивы байт ВСЕГДА за одинаковое время
        return CryptographicOperations.FixedTimeEquals(bytes1, bytes2);
    }
}