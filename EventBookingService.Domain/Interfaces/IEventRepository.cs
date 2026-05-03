using EventBookingService.Domain.Entities;

namespace EventBookingService.Domain.Interfaces;

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
}
