using System.ComponentModel.DataAnnotations;

using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Application.Services;
using EventBookingService.WebAPI.Models.Domain;
using EventBookingService.WebAPI.Models.DTO.Events;

using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.WebAPI.Controllers
{
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
        public async Task<IActionResult> GetAllEvents([FromQuery] EventsFilter filter, CancellationToken ct)
        {
            logger.LogDebug("Обработка запроса GET {methodName}", nameof(GetAllEvents));

            var result = await eventService.GetEventsAsync(filter, ct);
            return Ok(result);
        }

        /// <summary>
        /// Получить событие по id
        /// </summary>
        [HttpGet("{eventId:guid}")]
        [Tags("API для событий")]
        public async Task<IActionResult> GetEvent([Required]Guid eventId, CancellationToken ct)
        {
            logger.LogDebug("Обработка запроса GET {methodName} по id: {id} ", nameof(GetEvent), eventId);

            var responseEvent = await eventService.GetEventAsync(eventId, ct);
            return Ok(responseEvent);
        }

        /// <summary>
        /// Создать новое событие
        /// </summary>
        [HttpPost]
        [Tags("API для событий")]
        public async Task<IActionResult> CreateEvent([FromBody][Required]CreateEventDTO request, CancellationToken ct)
        {
            logger.LogDebug("Обработка запроса POST {methodName}", nameof(CreateEvent));

            var response = await eventService.CreateEventAsync(request, ct);
            return CreatedAtAction(nameof(CreateEvent), new { id = response.Id }, response);
        }

        /// <summary>
        /// Обновить событие целиком
        /// </summary>
        [HttpPut("{eventId:guid}")]
        [Tags("API для событий")]
        public async Task<IActionResult> ChangeEvent([Required]Guid eventId, [FromBody] UpdateEventDTO request, CancellationToken ct)
        {

            logger.LogDebug("Обработка запроса PUT {methodName} c id: {id}", nameof(ChangeEvent), eventId);

            await eventService.ChangeEventAsync(eventId, request, ct);
            return NoContent();
        }

        /// <summary>
        /// Удалить событие
        /// </summary>
        [HttpDelete("{eventId:guid}")]
        [Tags("API для событий")]
        public async Task<IActionResult> CancelEvent([Required]Guid eventId, CancellationToken ct)
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
        public async Task<IActionResult> CreateBook([Required]Guid eventId, CancellationToken ct)
        {
            logger.LogDebug("Обработка запроса POST {methodName}", nameof(CreateBook));

            var bookingDto = await bookingService.CreateBookingAsync(eventId, ct);

            return AcceptedAtAction(
                actionName: "GetBooking",
                controllerName: "Bookings",
                routeValues: new { bookingId = bookingDto.ID },
                value: bookingDto
            );
        }
    }
}
