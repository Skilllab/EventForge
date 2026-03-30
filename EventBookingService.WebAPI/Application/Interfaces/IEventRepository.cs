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
    /// <param name="ct"></param>
    Task AddAsync(Event @event, CancellationToken ct);

    /// <summary>
    /// Удаление события из репозитория
    /// </summary>
    /// <param name="id">ID события</param>
    /// <param name="ct"></param>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Получение события по ID
    /// </summary>
    /// <param name="id">ID события</param>
    /// <param name="ct"></param>
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Получение всех событий и возврат как AsQueryable, чтобы сервис мог накладывать фильтры
    /// </summary>
    IQueryable<Event> GetAll();

    /// <summary>
    /// Обновление события
    /// </summary>
    /// <param name="event">Само событие</param>
    /// <param name="ct"></param>
    Task UpdateAsync(Event @event, CancellationToken ct);
}
