using System.ComponentModel.DataAnnotations;

using EventForge.Events.Application.Interfaces;
using EventForge.Events.Infrastructure.Common;
using EventForge.Events.Presentation.DTO;
using EventForge.Events.Presentation.Mapping;
using EventForge.Users.Domain.Entities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Events.Presentation.Controllers
{
    /// <summary>
    /// Контроллер для событий
    /// </summary>
    /// <param name="eventService">Сервис для обработки событий</param>
    /// <param name="logger">Логгер</param>
    [Authorize(Policy = StringConstants.CustomJwtPolicy)]
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
    {
        /// <summary>
        /// Получить список всех событий
        /// </summary>
        [AllowAnonymous]
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
        [AllowAnonymous]
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
        [Authorize(Roles = nameof(RoleType.Admin))]
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
        [Authorize(Roles = nameof(RoleType.Admin))]
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
        [Authorize(Roles = nameof(RoleType.Admin))]
        [HttpDelete("{eventId:guid}")]
        [Tags("API для событий")]
        public async Task<IActionResult> CancelEvent([Required] Guid eventId, CancellationToken ct)
        {
            logger.LogDebug("Обработка запроса DELETE {methodName} с id: {id}", nameof(CancelEvent), eventId);

            await eventService.CancelEventAsync(eventId, ct);
            return NoContent();
        }
       
    }
}
