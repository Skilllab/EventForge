using EventBookingService.Domain.Entities;

namespace EventBookingService.Application.Interfaces;

/// <summary>
/// Основной интерфейс репозитория с пользователями
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Получить пользователя по Id
    /// </summary>
    /// <param name="id">Идентификатор пользователя</param>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>
    /// Получить пользователя по имени входа (логину)
    /// </summary>
    /// <param name="login">Имя входа (логин)</param>
    Task<User?> GetByLoginAsync(string login);

    /// <summary>
    /// Добавить пользователя
    /// </summary>
    /// <param name="user">Доменная модель пользователя</param>
    /// <returns></returns>
    Task AddAsync(User user);

    /// <summary>
    /// Проверка существования пользователя при регистрации
    /// </summary>
    /// <param name="login">Имя входа (логин)</param>
    Task<bool> ExistsAsync(string login);
}
