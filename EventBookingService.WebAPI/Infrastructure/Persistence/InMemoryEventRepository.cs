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
    public async Task AddAsync(Event @event)
    {
        await Task.FromResult(_events.TryAdd(@event.Id, @event));
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await Task.FromResult(_events.TryRemove(id, out _));
    }

    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid id)
    {
        _events.TryGetValue(id, out var @event);
        return await Task.FromResult(@event);
    }

    /// <inheritdoc/>
    public IQueryable<Event> GetAll()
    {
        return _events.Values.AsQueryable();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Event @event)
    {
        _events[@event.Id] = @event;
        await Task.CompletedTask;
    }
}
