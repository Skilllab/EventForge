using System.Net;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.Domain;
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
        public ApiResult<PaginatedResult> GetAllEvents([FromQuery] EventsFilter filter)
        {
            logger.LogDebug($"Обработка запроса GET {nameof(GetAllEvents)}");

            return new ApiResult<PaginatedResult>
            {
                Data = eventService.GetEvents(filter),
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Получены все события из базы согласно фильтрации"
            };
        }

        /// <summary>
        /// Получить событие по id
        /// </summary>
        [HttpGet("{id:guid}")]
        public ApiBaseResult GetEvent(Guid id)
        {
            logger.LogDebug($"Обработка запроса GET {nameof(GetEvent)}");

            var responseEvent = eventService.GetEvent(id);

            return new ApiResult<ResponseEventDTO>
            {
                Data = responseEvent,
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Событие найдено"
            };
        }

        /// <summary>
        /// Создать новое событие
        /// </summary>
        [HttpPost]
        public ApiBaseResult CreateEvent([FromBody] CreateEventDTO request)
        {
            logger.LogDebug($"Обработка запроса POST {nameof(CreateEvent)}");

            var response = eventService.CreateEvent(request);
            return new ApiResult<ResponseEventDTO>
            {
                Data = response,
                Success = true,
                StatusCode = HttpStatusCode.Created,
                Message = "Событие создано"
            };
        }

        /// <summary>
        /// Обновить событие целиком
        /// </summary>
        [HttpPut("{id:guid}")]
        public ApiResult ChangeEvent(Guid id, [FromBody] UpdateEventDTO request)
        {

            logger.LogDebug($"Обработка запроса PUT {nameof(ChangeEvent)}");

            eventService.ChangeEvent(id, request);
            return new ApiResult
            {
                Success = true,
                StatusCode = HttpStatusCode.NoContent,
                Message = "Событие обновлено"
            };
        }

        /// <summary>
        /// Удалить событие
        /// </summary>
        [HttpDelete("{id:guid}")]
        public ApiBaseResult CancelEvent(Guid id)
        {
            logger.LogDebug($"Обработка запроса DELETE {nameof(CancelEvent)}");

            eventService.CancelEvent(id);

            return new ApiResult
            {
                Success = true,
                StatusCode = HttpStatusCode.NoContent,
                Message = "Событие отменено"
            };
        }
    }
}
