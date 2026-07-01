using EventForge.Users.Domain.Entities;

namespace EventForge.Users.Application.Interfaces;

/// <summary>
/// Основной интерфейс репозитория с пользователями.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Получить пользователя по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <returns>Пользователь или null.</returns>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>
    /// Получить пользователя по имени входа (логину).
    /// </summary>
    /// <param name="login">Имя входа (логин).</param>
    /// <returns>Пользователь или null.</returns>
    Task<User?> GetByLoginAsync(string login);

    /// <summary>
    /// Добавить пользователя.
    /// </summary>
    /// <param name="user">Доменная модель пользователя.</param>
    Task AddAsync(User user);

    /// <summary>
    /// Проверка существования пользователя при регистрации.
    /// </summary>
    /// <param name="login">Имя входа (логин).</param>
    /// <returns>True, если пользователь существует.</returns>
    Task<bool> ExistsAsync(string login);
}
