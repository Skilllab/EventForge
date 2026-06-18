namespace EventBookingService.Infrastructure.Entities
{
    public class UserEntity
    {
        /// <summary>
        /// Уникальный идентификатор пользователя
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Коллекция бронирований пользователя
        /// </summary>
        public List<BookingEntity> Bookings { get; set; } = [];
        /// <summary>
        /// Имя входа пользователя
        /// </summary>
        public string Login { get; set; }
        /// <summary>
        /// Чэш пароля пользователя
        /// </summary>
        public string PasswordHash { get; set; }
        /// <summary>
        /// Роль пользователя
        /// </summary>
        public string Role { get; set; }
    }
}
