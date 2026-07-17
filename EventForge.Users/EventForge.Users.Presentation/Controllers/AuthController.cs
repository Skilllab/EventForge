using Asp.Versioning;

using EventForge.Users.Application.Interfaces;
using EventForge.Users.Presentation.DTO;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace EventForge.Users.Presentation.Controllers;

/// <summary>
/// Контроллер для аутентификации пользователей
/// </summary>
/// <param name="authService">Сервис аутентификации</param>
/// <param name="logger">Логгер</param>
[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IAuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Регистрирует нового пользователя с указанными логином, паролем и ролью.
    /// </summary>
    /// <response code="204">Пользователь успешно зарегистрирован.</response>
    /// <response code="400">Ошибка регистрации пользователя (логин занят или модель невалидна, пустой логин, короткий пароль и т.д.).</response>
    /// <param name="userRequest">
    /// Данные пользователя:
    /// <c>Login</c> - непустая строка, минимум 3 символа
    /// <c>Password</c> - непустая строка, минимум 6 символов.
    /// </param>
    /// <remarks>
    /// Пароль хешируется через BCrypt перед сохранением. В БД попадает только хеш,
    /// исходный пароль не хранится. Эндпоинт доступен анонимно.
    /// </remarks>
    [HttpPost("register")]
    [ApiVersion("1.0")]
    [SwaggerOperation(Summary = "Регистрация пользователя (Register)", Tags = new[] { "Auth" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest userRequest)
    {
        logger.LogDebug("Обработка запроса POST {methodName}. Регистрация нового пользователя: {login}", nameof(Register), userRequest.Login);

        if (await authService.RegisterUserAsync(userRequest.Login, userRequest.Password, userRequest.Role))
            return NoContent();

        return BadRequest(new { message = "Ошибка регистрации пользователя" });
    }

    /// <summary>
    /// Аутентифицирует пользователя по логину и паролю и возвращает JWT-токен. Login
    /// </summary>
    /// <param name="request">Данные для входа: логин и пароль.</param>
    /// <returns>
    /// <para><b>200 OK</b> — объект <c>{ "token": "..." }</c> с JWT-токеном.</para>
    /// <para><b>404 Not Found</b> — <c>{ "message": "Неверный логин или пароль." }</c>, если
    /// пользователь не существует или пароль не совпадает.</para>
    /// </returns>
    /// <remarks>
    /// Токен содержит claims: <c>sub</c> (ID пользователя), <c>role</c> (роль), Login
    /// а также стандартные <c>iat</c>, <c>exp</c>, <c>iss</c>, <c>aud</c>.
    /// Срок жизни токена задаётся в <c>JwtSettings:Lifetime</c> (в часах).
    /// </remarks>
    [HttpPost("login")]
    [ApiVersion("1.0")]
    [SwaggerOperation(Summary = "Аутентификация пользователя (Login)", Tags = new[] { "Auth" })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Login([FromBody] LoginDataRequest request)
    {
        logger.LogDebug("Обработка запроса POST {methodName}. Аутентификация пользователя: {login}", nameof(Login), request.Login);

        var token = await authService.LoginUserAsync(request.Login, request.Password);
        if (token is null)
            return NotFound(new { message = "Неверный логин или пароль." });

        return Ok(new { Token = token });
    }
}
