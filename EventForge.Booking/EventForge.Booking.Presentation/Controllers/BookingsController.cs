using System.ComponentModel.DataAnnotations;

using EventForge.Booking.Application.Interfaces;
using EventForge.Shared.Constants;
using EventForge.Shared.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EventForge.Booking.Presentation.Controllers;

/// <summary>
/// Контроллер для бронирования.
/// </summary>
[Authorize(Policy = StringConstants.CustomJwtPolicy)]
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class BookingsController(IBookingService bookingService, ILogger<BookingsController> logger) : ControllerBase
{
    /// <summary>
    /// Создать новое бронирование
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <param name="ct">Токен отмены</param>
    [HttpPost("{eventId:guid}")]
    [Tags("API для бронирования")]
    public async Task<IActionResult> CreateBooking([Required] Guid eventId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса POST {methodName}. Создание бронирования: {eventId}", nameof(CreateBooking), eventId);

        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdClaim?.Value) || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Не удалось определить идентификатор пользователя.");

        var bookingInfo = await bookingService.CreateBookingAsync(eventId, userId, ct);
        return Accepted(bookingInfo);
    }

    /// <summary>
    /// Получить информацию по бронированию
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования</param>
    /// <param name="ct">Токен отмены</param>
    [HttpGet("{bookingId:guid}")]
    [Tags("API для бронирования")]
    public async Task<IActionResult> GetBooking([Required] Guid bookingId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName}. Получение информации для бронирования: {bookingId}", nameof(GetBooking), bookingId);

        var bookingInfo = await bookingService.GetBookingByIdAsync(bookingId, ct);
        return Ok(bookingInfo);
    }

    /// <summary>
    /// Получить список бронирований
    /// </summary>
    /// <param name="ct">Токен отмены</param>
    [HttpGet]
    [Tags("API для бронирования")]
    public async Task<IActionResult> GetAllBooking(CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName}. Получение всех бронирований", nameof(GetBooking));

        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        var roleClaim = User.FindFirst("role");

        if (string.IsNullOrEmpty(userIdClaim?.Value) || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Не удалось определить идентификатор пользователя.");

        if (string.IsNullOrWhiteSpace(roleClaim?.Value) || !Enum.TryParse<RoleType>(roleClaim.Value, true, out var userRole))
            return Unauthorized("Не удалось определить роль пользователя.");

        var bookingInfo = await bookingService.GetAllBooking(userId, userRole, ct);
        return Ok(bookingInfo);
    }

    /// <summary>
    /// Удалить бронирование
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования</param>
    /// <param name="ct">Токен отмены</param>
    [HttpDelete("{bookingId:guid}")]
    [Tags("API для бронирования")]
    public async Task<IActionResult> CancelBooking([Required] Guid bookingId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса DELETE {methodName}. Удаление бронирования: {bookingId}", nameof(CancelBooking), bookingId);

        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        var roleClaim = User.FindFirst("role");

        if (string.IsNullOrEmpty(userIdClaim?.Value) || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Не удалось определить идентификатор пользователя.");

        if (string.IsNullOrWhiteSpace(roleClaim?.Value) || !Enum.TryParse<RoleType>(roleClaim.Value, true, out var userRole))
            return Unauthorized("Не удалось определить роль пользователя.");

        await bookingService.CancelBooking(bookingId, userId, userRole, ct);
        return NoContent();
    }
}
