using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.Domain;
using System.Collections.Concurrent;

namespace EventBookingService.WebAPI.Infrastructure.Persistence;

/// <summary>
/// Репозиторий для in-memory коллекции
/// </summary>
public class InMemoryEventRepository : IEventRepository
{
    private static readonly ConcurrentDictionary<Guid, Event> _events = new();

    /// <inheritdoc/>
    public void Add(Event @event)
    {
        _events.TryAdd(@event.Id, @event);
    }

    /// <inheritdoc/>
    public bool Delete(Guid id)
    {
        return _events.TryRemove(id, out _);
    }

    /// <inheritdoc/>
    public Event? GetById(Guid id)
    {
        _events.TryGetValue(id, out var @event);
        return @event;
    }

    /// <summary>
    /// Возвращаем как AsQueryable, чтобы сервис мог накладывать фильтры
    /// </summary>
    public IQueryable<Event> GetAll()
    {
        return _events.Values.AsQueryable();
    }

    /// <inheritdoc/>
    public void Update(Event @event)
    {
        _events[@event.Id] = @event;
    }
}