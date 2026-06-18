using System.ComponentModel.DataAnnotations;

using EventBookingService.Application.Interfaces;
using EventBookingService.Presentation.DTO;

using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class AuthController (IAuthService authService, ILogger<EventsController> logger) : ControllerBase
    {
        [HttpPost("/register")]
        [Tags("API по работе с пользователями")]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest userRequest)
        {
            if (await authService.RegisterUserAsync(userRequest.Login, userRequest.Password, userRequest.Role))
            {
                return NoContent();
            }

            return BadRequest(new { message = "Ошибка регистрации пользователя" });
        }

        [HttpPost("/login/{login}/{password}")]
        [Tags("API по работе с пользователями")]
        public async Task<IActionResult> Login([Required]string login, [Required]string password)
        {
            var token = await authService.LoginUserAsync(login, password);
            if (token == null)
                return Unauthorized(new { message = "Неверный логин или пароль." });

            return Ok(new { Token = token });
        }
    }
}
