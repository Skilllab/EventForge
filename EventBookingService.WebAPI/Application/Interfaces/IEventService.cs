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
    ResponseEventDTO CreateEvent(CreateEventDTO currentEvent);

    /// <summary>
    /// Отмена события
    /// </summary>
    /// <param name="eventId">ID события</param>
    void CancelEvent(Guid eventId);

    /// <summary>
    /// Получение событий с пагинацией
    /// </summary>
    /// <param name="filter">Фильтр для применения пагинации</param>
    PaginatedResult GetEvents(EventsFilter filter );

    /// <summary>
    /// Поиск события
    /// </summary>
    /// <param name="eventId">ID события</param>
    ResponseEventDTO GetEvent(Guid eventId);

    /// <summary>
    /// Изменение события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="currentEvent">Свойства из DTO для обновления события</param>
    void ChangeEvent(Guid eventId, UpdateEventDTO currentEvent);
}