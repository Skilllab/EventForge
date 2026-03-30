using EventBookingService.WebAPI.Models.DTO;

namespace EventBookingService.WebAPI.Application.Interfaces;

/// <summary>
/// Основной интерфейс сервиса
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Создание события
    /// </summary>
    /// <param name="currentEvent">Входящая DTO</param>
    Task<ResponseEventDTO> CreateEventAsync(CreateEventDTO currentEvent);

    /// <summary>
    /// Отмена события
    /// </summary>
    /// <param name="eventId">ID события</param>
    Task CancelEventAsync(Guid eventId);

    /// <summary>
    /// Получение событий с пагинацией
    /// </summary>
    /// <param name="filter">Фильтр для применения пагинации</param>
    Task<PaginatedResult> GetEventsAsync(EventsFilter filter );

    /// <summary>
    /// Поиск события
    /// </summary>
    /// <param name="eventId">ID события</param>
    Task<ResponseEventDTO> GetEventAsync(Guid eventId);

    /// <summary>
    /// Изменение события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="currentEvent">Свойства из DTO для обновления события</param>
    Task ChangeEventAsync(Guid eventId, UpdateEventDTO currentEvent);
}
