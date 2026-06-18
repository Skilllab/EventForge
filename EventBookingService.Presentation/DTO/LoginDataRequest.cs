using System.ComponentModel.DataAnnotations;

namespace EventBookingService.Presentation.DTO
{
    /// <summary>
    /// Запрос на вход пользователя
    /// </summary>
    public class LoginDataRequest
    {
        /// <summary>
        /// Логин пользователя
        /// </summary>
        [Required(ErrorMessage = "Логин обязателен")]
        public string Login { get; set; }

        /// <summary>
        /// Пароль пользователя
        /// </summary>
        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; }
    }
}
