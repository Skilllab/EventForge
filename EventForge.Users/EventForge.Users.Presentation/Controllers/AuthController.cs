using EventForge.Users.Application.Interfaces;
using EventForge.Users.Presentation.DTO;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Users.Presentation.Controllers;

/// <summary>
/// Контроллер для аутентификации пользователей.
/// </summary>
/// <param name="authService">Сервис аутентификации.</param>
/// <param name="logger">Логгер.</param>
[AllowAnonymous]
[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Регистрация нового пользователя.
    /// </summary>
    /// <param name="userRequest">Данные пользователя для регистрации.</param>
    /// <returns>Результат регистрации.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest userRequest)
    {
        logger.LogDebug("Обработка запроса POST {methodName}. Регистрация нового пользователя: {login}", nameof(Register), userRequest.Login);

        if (await authService.RegisterUserAsync(userRequest.Login, userRequest.Password, userRequest.Role))
            return NoContent();

        return BadRequest(new { message = "Ошибка регистрации пользователя" });
    }

    /// <summary>
    /// Аутентификация пользователя и получение JWT-токена.
    /// </summary>
    /// <param name="request">Данные для аутентификации пользователя.</param>
    /// <returns>JWT-токен.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDataRequest request)
    {
        logger.LogDebug("Обработка запроса POST {methodName}. Аутентификация пользователя: {login}", nameof(Login), request.Login);

        var token = await authService.LoginUserAsync(request.Login, request.Password);
        if (token is null)
            return NotFound(new { message = "Неверный логин или пароль." });

        return Ok(new { Token = token });
    }
}
