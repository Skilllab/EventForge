using System.ComponentModel.DataAnnotations;

using EventBookingService.Application.Interfaces;
using EventBookingService.Presentation.DTO;
using EventBookingService.Presentation.Mapping;

using Microsoft.AspNetCore.Mvc;


namespace EventBookingService.Presentation.Controllers;

/// <summary>
/// Контроллер для событий
/// </summary>
/// <param name="eventService">Сервис для обработки событий</param>
/// <param name="bookingService">Сервис для обработки бронирований</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EventsController(IEventService eventService, IBookingService bookingService, ILogger<EventsController> logger) : ControllerBase
{
    /// <summary>
    /// Получить список всех событий
    /// </summary>
    [HttpGet]
    [Tags("API для событий")]
    public async Task<IActionResult> GetAllEvents([FromQuery] EventsFilterRequest filterRequest, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName}", nameof(GetAllEvents));

        var result = await eventService.GetEventsAsync(filterRequest.ToAppDto(), ct);
        return Ok(result.ToWebDto());
    }

    /// <summary>
    /// Получить событие по id
    /// </summary>
    [HttpGet("{eventId:guid}")]
    [Tags("API для событий")]
    public async Task<IActionResult> GetEvent([Required] Guid eventId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName} по id: {id} ", nameof(GetEvent), eventId);

        var responseEvent = await eventService.GetEventAsync(eventId, ct);
        return Ok(responseEvent.ToWebDto());
    }

    /// <summary>
    /// Создать новое событие
    /// </summary>
    [HttpPost]
    [Tags("API для событий")]
    public async Task<IActionResult> CreateEvent([FromBody][Required] CreateEventRequest request, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса POST {methodName}", nameof(CreateEvent));

        var response = await eventService.CreateEventAsync(request.ToAppDto(), ct);
        return CreatedAtAction(nameof(CreateEvent), new { id = response.Id }, response.ToWebDto());
    }

    /// <summary>
    /// Обновить событие целиком
    /// </summary>
    [HttpPut("{eventId:guid}")]
    [Tags("API для событий")]
    public async Task<IActionResult> ChangeEvent([Required] Guid eventId, [FromBody] UpdateEventRequest request, CancellationToken ct)
    {

        logger.LogDebug("Обработка запроса PUT {methodName} c id: {id}", nameof(ChangeEvent), eventId);

        await eventService.ChangeEventAsync(eventId, request.ToAppDto(), ct);
        return NoContent();
    }

    /// <summary>
    /// Удалить событие
    /// </summary>
    [HttpDelete("{eventId:guid}")]
    [Tags("API для событий")]
    public async Task<IActionResult> CancelEvent([Required] Guid eventId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса DELETE {methodName} с id: {id}", nameof(CancelEvent), eventId);

        await eventService.CancelEventAsync(eventId, ct);
        return NoContent();
    }

    /// <summary>
    /// Создать новое бронирование
    /// </summary>
    [HttpPost("{eventId:guid}/book")]
    [Tags("API для бронирования")]
    public async Task<IActionResult> CreateBook([Required] Guid eventId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса POST {methodName}", nameof(CreateBook));

        var bookingDto = await bookingService.CreateBookingAsync(eventId, Guid.NewGuid(), ct);

        return AcceptedAtAction(
            actionName: "GetBooking",
            controllerName: "Bookings",
            routeValues: new { bookingId = bookingDto.ID },
            value: bookingDto
        );
    }
}
