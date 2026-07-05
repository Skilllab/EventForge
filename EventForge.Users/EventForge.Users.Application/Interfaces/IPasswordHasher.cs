namespace EventForge.Users.Application.Interfaces;

/// <summary>
/// Компонент хэширования паролей.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Создать хэш пароля.
    /// </summary>
    /// <param name="password">Пароль.</param>
    /// <returns>Хэш пароля.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Проверить пароль на соответствие хэшу.
    /// </summary>
    /// <param name="password">Пароль.</param>
    /// <param name="hash">Хэш.</param>
    /// <returns>True, если пароль соответствует хэшу.</returns>
    bool VerifyPassword(string password, string hash);
}
