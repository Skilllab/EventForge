using EventBookingService.WebAPI.Models.Domain;

namespace EventBookingService.WebAPI.Application.Interfaces;

/// <summary>
/// Основной интерфейс репозитория
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Добавление события в репозиторий
    /// </summary>
    /// <param name="event">Само событие</param>
    /// <param name="ct">Токен отмены</param>
    Task AddAsync(Event @event, CancellationToken ct);

    /// <summary>
    /// Удаление события из репозитория
    /// </summary>
    /// <param name="id">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Получение события по ID
    /// </summary>
    /// <param name="id">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Обновление события
    /// </summary>
    /// <param name="event">Само событие</param>
    /// <param name="ct">Токен отмены</param>
    Task UpdateAsync(Event @event, CancellationToken ct);


    /// <summary>
    /// Получение всех событий с фильтрацией и пагинацией
    /// </summary>
    /// <param name="query">Предикат для фильтрации событий</param>
    /// <param name="page">Номер страницы с данными для возврата</param>
    /// <param name="pageSize">Количество элементов на странице</param>
    /// <param name="ct">Токен отмены</param>
    List<Event> GetAll(Func<Event, bool> query, int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Получение общего количества элементов в базе
    /// </summary>
    /// <param name="ct">Токен отмены</param>

    long GetTotalCount(CancellationToken ct);
}
