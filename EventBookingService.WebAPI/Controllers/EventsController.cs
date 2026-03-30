using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
    {
        /// <summary>
        /// Получить список всех событий
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllEvents([FromQuery] EventsFilter filter)
        {
            logger.LogDebug("Обработка запроса GET {methodName}", nameof(GetAllEvents));

            var result = await eventService.GetEventsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Получить событие по id
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEvent(Guid id)
        {
            logger.LogDebug("Обработка запроса GET {methodName} по id: {id} ", nameof(GetEvent), id);

            var responseEvent = await eventService.GetEventAsync(id);
            return Ok(responseEvent);
        }

        /// <summary>
        /// Создать новое событие
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDTO request)
        {
            logger.LogDebug("Обработка запроса POST {methodName}", nameof(CreateEvent));

            var response = await eventService.CreateEventAsync(request);
            return CreatedAtAction(nameof(CreateEvent), new { id = response.Id }, response);
        }

        /// <summary>
        /// Обновить событие целиком
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> ChangeEvent(Guid id, [FromBody] UpdateEventDTO request)
        {

            logger.LogDebug("Обработка запроса PUT {methodName} c id: {id}", nameof(ChangeEvent), id);

            await eventService.ChangeEventAsync(id, request);
            return NoContent();
        }

        /// <summary>
        /// Удалить событие
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> CancelEvent(Guid id)
        {
            logger.LogDebug("Обработка запроса DELETE {methodName} с id: {id}", nameof(CancelEvent), id);

            await eventService.CancelEventAsync(id);
            return NoContent();
        }
    }
}
