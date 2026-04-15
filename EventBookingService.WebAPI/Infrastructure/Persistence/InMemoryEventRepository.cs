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
    public async Task AddAsync(Event @event, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Task.FromResult(_events.TryAdd(@event.Id, @event));
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return await Task.FromResult(_events.TryRemove(id, out _));
    }

    /// <inheritdoc/>
    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _events.TryGetValue(id, out var @event);
        return await Task.FromResult(@event);
    }

    /// <inheritdoc/>
    public List<Event> GetAll(Func<Event, bool> query, int page, int pageSize, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (page == 0)
            throw new ArgumentException("Номер страницы пагинации не может быть менье 1");

        if (pageSize==0 && pageSize >100 )
            throw new ArgumentException("Размер страницы должен быть в пределах от 1 до 100");

        return _events.Values.Where(query)
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Event @event, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _events[@event.Id] = @event;
        await Task.CompletedTask;
    }

    public long GetTotalCount(CancellationToken ct)
    {
        return  _events.LongCount();
    }

}
