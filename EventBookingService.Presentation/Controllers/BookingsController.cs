using System.ComponentModel.DataAnnotations;

using EventBookingService.Application.Interfaces;
using EventBookingService.Presentation.Mapping;

using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.Presentation.Controllers;

/// <summary>
/// Контроллер для бронирования
/// </summary>
/// <param name="bookingService">Сервис для обработки бронирований</param>
/// <param name="logger">Логгер</param>
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
}
