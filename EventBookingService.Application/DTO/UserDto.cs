using EventBookingService.Domain.Entities;

namespace EventBookingService.Application.DTO;

/// <summary>
/// DTO пользователя
/// </summary>
public class UserDTO
{
    /// <summary>
    /// Имя входа пользователя
    /// </summary>
    public string Login { get; set; }
    /// <summary>
    /// Пароль пользователя
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// Роль пользователя
    /// </summary>
    public RoleType Role { get; set; }
}
