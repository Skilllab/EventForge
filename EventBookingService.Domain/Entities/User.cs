using EventBookingService.Domain.Exceptions;

namespace EventBookingService.Domain.Entities
{
    /// <summary>
    /// Модель пользователя
    /// </summary>
    public class User
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
        /// <summary>
        /// Коллекция бронирований пользователя
        /// </summary>
        public ICollection<Booking> Bookings { get; private set; } = [];

        private User(string login, string passwordHash, RoleType role)
        {
            Id = Guid.NewGuid();
            Login = login;
            PasswordHash = passwordHash;
            Role = role;
        }

        public static User Create(string login, string passwordHash, RoleType role)
        {
            if (string.IsNullOrEmpty(login))
                throw new ValidationCustomException(nameof(User), Guid.Empty.ToString(), "Логин пользователя не может быть пустым.");

            if (string.IsNullOrEmpty(passwordHash))
                throw new ValidationCustomException(nameof(User), Guid.Empty.ToString(), "Пароль пользователя не может быть пустым.");

            return new User(login, passwordHash, role);
        }

    }
}
