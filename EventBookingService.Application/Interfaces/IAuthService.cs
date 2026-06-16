using EventBookingService.Domain.Entities;

namespace EventBookingService.Application.Interfaces;

/// <summary>
/// Сервис регистрации пользователя
/// </summary>
public interface  IAuthService
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    /// <param name="login">Имя входа (логин)</param>
    /// <param name="password">Пароль</param>
    /// <param name="role">Роль</param>
    /// <returns></returns>
    Task<bool> RegisterUserAsync(string login, string password, RoleType role);

    /// <summary>
    /// Вход пользователя
    /// </summary>
    /// <param name="login">Имя входа (логин)</param>
    /// <param name="password">Пароль</param>
    Task<string> LoginUserAsync(string login, string password);

}
