using EventBookingService.Application.Interfaces;
using EventBookingService.Presentation.DTO;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.Presentation.Controllers
{
    /// <summary>
    /// Контроллер для аутентификации пользователей
    /// </summary>
    /// <param name="authService">Сервис аутентификации</param>
    /// <param name="logger">Логгер</param>
    [AllowAnonymous]
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class AuthController (IAuthService authService, ILogger<EventsController> logger) : ControllerBase
    {
        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        /// <param name="userRequest">Данные пользователя для регистрации</param>
        [HttpPost("/auth/register")]
        [Tags("API по работе с пользователями")]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest userRequest)
        {
            logger.LogDebug("Обработка запроса POST {methodName}. Регистрация нового пользователя: {login}", nameof(Register), userRequest.Login);

            if (await authService.RegisterUserAsync(userRequest.Login, userRequest.Password, userRequest.Role))
                return NoContent();

            return BadRequest(new { message = "Ошибка регистрации пользователя" });
        }

        /// <summary>
        /// Аутентификация пользователя и получение JWT токена
        /// </summary>
        /// <param name="request">Данные для аутентификации пользователя</param>
        /// <returns></returns>
        [HttpPost("/auth/login")]
        [Tags("API по работе с пользователями")]
        public async Task<IActionResult> Login([FromBody] LoginDataRequest request)
        {
            logger.LogDebug("Обработка запроса POST {methodName}. Аутентификация пользователя: {login}", nameof(Login), request.Login);

            var token = await authService.LoginUserAsync(request.Login, request.Password);
            if (token == null)
                return NotFound(new { message = "Неверный логин или пароль." });

            return Ok(new { Token = token });
        }
    }
}
