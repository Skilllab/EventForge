using EventBookingService.WebAPI.Models.Domain;

namespace EventBookingService.WebAPI.Application.Interfaces;

/// <summary>
/// Основной интерфейс репозитория
/// </summary>
public interface IEventRepository
{
    void Add(Event @event);

    bool Delete(Guid id);

    Event? GetById(Guid id);

    IQueryable<Event> GetAll();

    void Update(Event @event);
}