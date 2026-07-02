using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;

using EventBookingService.Application.Interfaces;
using EventBookingService.Domain.Entities;
using EventBookingService.Infrastructure.Common;
using EventBookingService.Presentation.Mapping;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.Presentation.Controllers;

/// <summary>
/// Контроллер для бронирования
/// </summary>
/// <param name="bookingService">Сервис для обработки бронирований</param>
/// <param name="logger">Логгер</param>
[Authorize(Policy = StringConstants.CustomJwtPolicy)]
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class BookingsController(IBookingService bookingService, ILogger<BookingsController> logger) : ControllerBase
{
    /// <summary>
    /// Получить информацию по бронированию
    /// </summary>
    [HttpGet("{bookingId:guid}")]
    [Tags("API для бронирования")]
    public async Task<IActionResult> GetBooking([Required] Guid bookingId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName}. Получение информации для бронирования: {bookingId}", nameof(GetBooking), bookingId);

        var bookingInfo = await bookingService.GetBookingByIdAsync(bookingId, ct);

        return Ok(bookingInfo.ToWebDto());
    }

    /// <summary>
    /// Удалить бронирование
    /// </summary>
    [HttpDelete("{bookingId:guid}")]
    [Tags("API для бронирования")]
    public async Task<IActionResult> CancelBooking([Required] Guid bookingId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса DELETE {methodName}. Удаление бронирования: {bookingId}", nameof(CancelBooking), bookingId);

        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        var roleClaim = User.FindFirst("role");


        if (string.IsNullOrEmpty(userIdClaim?.Value) || !Guid.TryParse(userIdClaim.Value, out Guid userId))
        {
            return Unauthorized("Не удалось определить идентификатор пользователя.");
        }

        if (string.IsNullOrWhiteSpace(roleClaim?.Value) || !Enum.TryParse<RoleType>(roleClaim.Value, true, out var userRole))
            return Unauthorized("Не удалось определить роль пользователя.");


        await bookingService.CancelBooking(bookingId, userId, userRole, ct);
        return NoContent();
    }
}
