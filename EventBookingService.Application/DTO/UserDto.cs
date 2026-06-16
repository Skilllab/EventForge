using EventBookingService.Domain.Entities;

namespace EventBookingService.Application.DTO
{
    /// <summary>
    /// DTO пользователя
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Уникальный идентификатор пользователя
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Имя входа пользователя
        /// </summary>
        public string Login { get; private set; }
        /// <summary>
        /// Чэш пароля пользователя
        /// </summary>
        public string PasswordHash { get; private set; }
        /// <summary>
        /// Роль пользователя
        /// </summary>
        public RoleType Role { get; private set; }

    }
}
