using EventForge.Events.Domain.Entities;

namespace EventForge.Events.Application.Interfaces;

/// <summary>
/// Основной интерфейс репозитория с событиями
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Получение события по ID
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <param name="ct">Токен отмены</param>
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Получить выборку с событиями
    /// </summary>
    /// <param name="title">Поиск с учетом наименования</param>
    /// <param name="startAt">Поиск с учетом даты начала события</param>
    /// <param name="endAt">Поиск с учетом даты окончания события</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Количество событий на странице</param>
    /// <param name="ct">Токен отмены</param>
    Task<PagedResult<Event>> GetPagedAsync(string? title,
        DateTime? startAt,
        DateTime? endAt,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>
    /// Добавить событие
    /// </summary>
    /// <param name="event">Доменная модель события</param>
    /// <param name="ct">Токен отмены</param>
    Task AddAsync(Event @event, CancellationToken ct);

    /// <summary>
    /// Обновить событие
    /// </summary>
    /// <param name="event">Доменная модель события</param>
    /// <param name="ct">Токен отмены</param>
    Task UpdateAsync(Event @event, CancellationToken ct);

    /// <summary>
    /// Удалить событие
    /// </summary>
    /// <param name="id">GUID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Атомарно пытается уменьшить количество свободных мест на событии.
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <param name="seatsCount">Количество мест для бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> TryReserveSeatAsync(Guid eventId, int seatsCount, CancellationToken ct);

    /// <summary>
    /// Атомарно освобождает места на событии.
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <param name="seatsCount">Количество мест для освобождения</param>
    /// <param name="ct">Токен отмены</param>
    Task ReleaseSeatAsync(Guid eventId, int seatsCount, CancellationToken ct);
}
