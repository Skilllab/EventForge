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
        public IActionResult GetAllEvents([FromQuery] EventsFilter filter)
        {
            logger.LogDebug("Обработка запроса GET {methodName}", nameof(GetAllEvents));

            var result = eventService.GetEvents(filter);
            return Ok(result);
        }

        /// <summary>
        /// Получить событие по id
        /// </summary>
        [HttpGet("{id:guid}")]
        public IActionResult GetEvent(Guid id)
        {
            logger.LogDebug("Обработка запроса GET {methodName} по id: {id} ", nameof(GetEvent), id);

            var responseEvent = eventService.GetEvent(id);
            return Ok(responseEvent);
        }

        /// <summary>
        /// Создать новое событие
        /// </summary>
        [HttpPost]
        public IActionResult CreateEvent([FromBody] CreateEventDTO request)
        {
            logger.LogDebug("Обработка запроса POST {methodName}", nameof(CreateEvent));

            var response = eventService.CreateEvent(request);
            return CreatedAtAction(nameof(CreateEvent), new { id = response.Id }, response);
        }

        /// <summary>
        /// Обновить событие целиком
        /// </summary>
        [HttpPut("{id:guid}")]
        public IActionResult ChangeEvent(Guid id, [FromBody] UpdateEventDTO request)
        {

            logger.LogDebug("Обработка запроса PUT {methodName} c id: {id}", nameof(ChangeEvent), id);

            eventService.ChangeEvent(id, request);
            return NoContent();
        }

        /// <summary>
        /// Удалить событие
        /// </summary>
        [HttpDelete("{id:guid}")]
        public IActionResult CancelEvent(Guid id)
        {
            logger.LogDebug("Обработка запроса DELETE {methodName} с id: {id}", nameof(CancelEvent), id);

            eventService.CancelEvent(id);
            return NoContent();
        }
    }
}
