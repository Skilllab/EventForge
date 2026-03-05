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
    public class EventController(IEventService eventService, ILogger<EventController> logger) : ControllerBase
    {
        /// <summary>
        /// Получить список всех событий
        /// </summary>
        [HttpGet]
        public ApiBaseResult GetAllEvents()
        {
            logger.LogDebug($"Обработка запроса GET {nameof(GetAllEvents)}");

            var response = eventService.GetEvents().Select(MapToDTO);

            if (!response.Any())
            {
                return new ApiBaseResult()
                {
                    Success = false,
                    StatusCode = HttpStatusCode.NoContent,
                    Message = "В базе нет заданий"
                };
            }


            return new ApiResult<IEnumerable<ResponceEventDTO>>
            {
                Data = response,
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Получены все задания из базы"
            };
        }

        /// <summary>
        /// Получить событие по id
        /// </summary>
        [HttpGet("{id}")]
        public ApiResult<ResponceEventDTO> GetEvent(Guid id)
        {
            logger.LogDebug($"Обработка запроса GET {nameof(GetEvent)}");


            if (eventService.GetEvent(id, out var @event))
            {
                return new ApiResult<ResponceEventDTO>
                {
                    Data = MapToDTO(@event),
                    Success = true,
                    StatusCode = HttpStatusCode.OK,
                    Message = "Задание найдено"
                };
            }

            return new ApiResult<ResponceEventDTO>
            {
                Data = null,
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = "Задание не найдено"
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
                    Message = "Задание создано"
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
                    Message = "Задание создано"
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
                Message = "Задание отменено"
            };
        }


        /// <summary>
        /// Метод для мапирования из события в DTO 
        /// </summary>
        /// <param name="currentEvent">Доменное событие</param>
        /// <returns>DTO для отправки</returns>
        private ResponceEventDTO MapToDTO(Event currentEvent)
        {
            return new ResponceEventDTO()
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
