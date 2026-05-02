using EventBookingService.Data.Context;
using EventBookingService.Data.Mapping;
using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace EventBookingService.Data.Repositories;

public class EventRepository(IDbContextFactory<AppDbContext> factory) : IEventRepository
{
    
    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        var entity =  await context.Events
            .Include(e => e.Bookings)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return entity?.ToDomain();
    }

    public async Task<PagedResult<Event>> GetPagedAsync(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        // Валидация
        if (page <= 0) throw new ArgumentException("Номер страницы пагинации не может быть менье 1");
        if (pageSize <= 0 || pageSize > 100) throw new ArgumentException("Размер страницы должен быть в пределах от 1 до 100");

        await using var context = await factory.CreateDbContextAsync(ct);

        // 1. Создаем базовый запрос с фильтром
        var query = context.Events.AsNoTracking();

        if (!string.IsNullOrEmpty(title)) query = query.Where(e => e.Title == title);

        if (startAt.HasValue) query = query.Where(e => e.StartAt >= startAt);

        if (endAt.HasValue)
        {
            // Если время не указано (00:00:00), значит ищем до конца дня включительно
            query = endAt.Value.TimeOfDay == TimeSpan.Zero
                ? query.Where(e => e.EndAt.Date <= endAt.Value.Date)
                : query.Where(e=>e.EndAt <= endAt.Value);
        }

        var totalCount = await context.Events.LongCountAsync(ct);

        var entities = await query
            .OrderBy(e => e.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var domainEvents = entities
            .Select(e => e.ToDomain())
            .ToList();

        return new PagedResult<Event>(domainEvents, totalCount);
    }

    public async Task AddAsync(Event @event, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        var entity = @event.ToEntity();
        await context.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Event @event, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        var entity = @event.ToEntity();
        context.Events.Update(entity);
        await context.SaveChangesAsync(ct);
    }


    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        var affected = await context.Events.Where(e => e.Id == id).ExecuteDeleteAsync(ct);
        return affected > 0;
    }
}
