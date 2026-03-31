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
    /// <param name="ct">Токен отмены</param>
    Task<ResponseEventDTO> CreateEventAsync(CreateEventDTO currentEvent, CancellationToken ct);

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
    Task<PaginatedResult> GetEventsAsync(EventsFilter filter, CancellationToken ct);

    /// <summary>
    /// Поиск события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<ResponseEventDTO> GetEventAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Изменение события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="currentEvent">Свойства из DTO для обновления события</param>
    /// <param name="ct">Токен отмены</param>
    Task ChangeEventAsync(Guid eventId, UpdateEventDTO currentEvent, CancellationToken ct);
}
