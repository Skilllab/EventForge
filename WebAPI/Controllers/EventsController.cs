using Microsoft.AspNetCore.Mvc;
using System.Net;
using WebAPI.Application.Interfaces;
using WebAPI.Models.Domain;
using WebAPI.Models.DTO;


namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
    {
        /// <summary>
        /// Получить список всех событий
        /// </summary>
        [HttpGet]
        public ApiResult<IEnumerable<ResponseEventDTO>> GetAllEvents()
        {
            logger.LogDebug($"Обработка запроса GET {nameof(GetAllEvents)}");

            return new ApiResult<IEnumerable<ResponseEventDTO>>
            {
                Data = eventService.GetEvents(),
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Получены все события из базы"
            };
        }

        /// <summary>
        /// Получить событие по id
        /// </summary>
        [HttpGet("{id}")]
        public ApiBaseResult GetEvent(Guid id)
        {
            logger.LogDebug($"Обработка запроса GET {nameof(GetEvent)}");


            if (eventService.GetEvent(id, out var responseEvent))
            {
                return new ApiResult<ResponseEventDTO>
                {
                    Data = responseEvent,
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Событие найдено"
                };
            }

            return new ApiResult<ResponseEventDTO>
            {
                Data = null,
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = "Событие не найдено"
            };
        }

        /// <summary>
        /// Создать новое событие
        /// </summary>
        [HttpPost]
        public ApiBaseResult CreateEvent([FromBody] CreateEventDTO request)
        {

            logger.LogDebug($"Обработка запроса POST {nameof(CreateEvent)}");

            try
            {
                var response = eventService.CreateEvent(request);
                return new ApiResult<ResponseEventDTO>
                {
                    Data = response,
                    Success = true,
                    StatusCode = HttpStatusCode.Created,
                    Message = "Событие создано"
                };
            }
            catch (Exception e)
            {
                return new ApiResult
                {
                    Success = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = e.Message
                };
            }
        }

        /// <summary>
        /// Обновить событие целиком
        /// </summary>
        [HttpPut]
        public ApiResult ChangeEvent([FromBody] EventDto request)
        {
            logger.LogDebug($"Обработка запроса PUT {nameof(ChangeEvent)}");

            try
            {
                //eventService.ChangeEvent(MapToEvent(request));
                return new ApiResult
                {
                    Success = true,
                    StatusCode = HttpStatusCode.NoContent,
                    Message = "Событие обновлено"
                };
            }
            catch (Exception e)
            {
                return new ApiResult
                {
                    Success = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = e.Message
                };
            }
        }


        /// <summary>
        /// Удалить событие
        /// </summary>
        [HttpDelete("{id}")]
        public ApiBaseResult CancelEvent(Guid id)
        {
            logger.LogDebug($"Обработка запроса DELETE {nameof(CancelEvent)}");

            if (!eventService.CancelEvent(id))
                return new ApiResult
                {
                    Success = true,
                    StatusCode = HttpStatusCode.NotFound,
                    Message = $"Событие с ID: {id} не найдено"
                };

            return new ApiResult
            {
                Success = true,
                StatusCode = HttpStatusCode.NoContent,
                Message = "Собтыие отменено"
            };
        }
    }
}
