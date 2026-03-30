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
    void Add(Event @event);

    /// <summary>
    /// Удаление события из репозитория
    /// </summary>
    /// <param name="id">ID события</param>
    bool Delete(Guid id);

    /// <summary>
    /// Получение события по ID
    /// </summary>
    /// <param name="id">ID события</param>
    Event? GetById(Guid id);

    /// <summary>
    /// Получение всех событий и возврат как AsQueryable, чтобы сервис мог накладывать фильтры
    /// </summary>
    IQueryable<Event> GetAll();

    /// <summary>
    /// Обновление события
    /// </summary>
    /// <param name="event">Само событие</param>
    void Update(Event @event);
}