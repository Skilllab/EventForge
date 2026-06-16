namespace EventBookingService.Infrastructure.Common
{
    /// <summary>
    /// Класс для настроек генерации токена
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// Секрет
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Издатель
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Получатель
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Время жизни токена в часах
        /// </summary>
        public int Lifetime { get; set; }
    }

}
