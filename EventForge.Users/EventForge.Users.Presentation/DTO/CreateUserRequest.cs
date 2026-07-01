using System.ComponentModel.DataAnnotations;

namespace EventForge.Users.Presentation.DTO;

/// <summary>
/// Запрос создания пользователя.
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Логин пользователя.
    /// </summary>
    [Required(ErrorMessage = "Имя входа (логин) обязательно для заполнения.")]
    [StringLength(64, MinimumLength = 3)]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Пароль пользователя.
    /// </summary>
    [Required(ErrorMessage = "Пароль обязателен для заполнения.")]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Роль пользователя.
    /// </summary>
    public string? Role { get; set; }
}
