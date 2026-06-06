
using EventBookingService.Application.DTO;

namespace EventBookingService.Application.Interfaces;

/// <summary>
/// Основной интерфейс сервиса
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Создание события
    /// </summary>
    /// <param name="currentEvent">Входящая DTO</param>
    /// <param name="ct">Токен отмены</param>
    Task<EventDto> CreateEventAsync(CreateEventDto currentEvent, CancellationToken ct);

    /// <summary>
    /// Отмена события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task CancelEventAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Получение событий с пагинацией
    /// </summary>
    /// <param name="filter">Фильтр для применения пагинации</param>
    /// <param name="ct">Токен отмены</param>
    Task<PaginatedResultDto> GetEventsAsync(EventsFilterDto filter, CancellationToken ct);

    /// <summary>
    /// Поиск события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<EventDto> GetEventAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Изменение события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="currentEvent">Свойства из DTO для обновления события</param>
    /// <param name="ct">Токен отмены</param>
    Task ChangeEventAsync(Guid eventId, UpdateEventDto currentEvent, CancellationToken ct);
}
