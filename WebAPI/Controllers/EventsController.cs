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

            var response = eventService.GetEvents().Select(MapToDTO);

            return new ApiResult<IEnumerable<ResponseEventDTO>>
            {
                Data = response,
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Получены все события из базы"
            };
        }

        /// <summary>
        /// Получить событие по id
        /// </summary>
        [HttpGet("{id}")]
        public ApiResult<ResponseEventDTO> GetEvent(Guid id)
        {
            logger.LogDebug($"Обработка запроса GET {nameof(GetEvent)}");


            if (eventService.GetEvent(id, out var @event))
            {
                return new ApiResult<ResponseEventDTO>
                {
                    Data = MapToDTO(@event),
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
        public ApiResult CreateEvent([FromBody] EventDto request)
        {

            logger.LogDebug($"Обработка запроса POST {nameof(CreateEvent)}");

            try
            {
                eventService.CreateEvent(MapToEvent(request));

                return new ApiResult
                {
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
                eventService.ChangeEvent(MapToEvent(request));
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


        /// <summary>
        /// Метод для мапирования из события в DTO 
        /// </summary>
        /// <param name="currentEvent">Доменное событие</param>
        /// <returns>DTO для отправки</returns>
        private ResponseEventDTO MapToDTO(Event currentEvent)
        {
            return new ResponseEventDTO()
            {
                Id = currentEvent.Id,
                Title = currentEvent.Title,
                Description = currentEvent.Description,
                StartAt = currentEvent.StartAt,
                EndAt = currentEvent.EndAt
            };
        }

        /// <summary>
        ///  Метод для мапирования из DTO в событие
        /// </summary>
        /// <param name="currentEvent">DTO событие</param>
        /// <returns>Доменное событие</returns>
        private Event MapToEvent(EventDto currentEvent)
        {
            return new Event()
            {
                Id = currentEvent.Id,
                Title = currentEvent.Title,
                Description = currentEvent.Description,
                StartAt = currentEvent.StartAt,
                EndAt = currentEvent.EndAt
            };
        }
    }
}
