using System.ComponentModel.DataAnnotations;

using EventBookingService.Presentation.ValidationAttributes;

namespace EventBookingService.Presentation.DTO
{
    /// <summary>
    /// Запрос создания пользователя
    /// </summary>
    public class CreateUserRequest
    {
        /// <summary>
        /// Логин пользователя
        /// </summary>
        [Required(ErrorMessage = "Имя входа (логин) обязательно для заполнения.")]
        public string Login{ get; set; }

        /// <summary>
        /// Пароль пользователя
        /// </summary>
        [Required(ErrorMessage = "Пароль обязателен для заполнения.")]
        public string Password{ get; set; }

        /// <summary>
        /// Роль пользователя
        /// </summary>
        [RoleValidation]
        public string? Role{ get; set; }
    }
}
