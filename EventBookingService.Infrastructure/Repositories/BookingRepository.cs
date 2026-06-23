using EventBookingService.Application.Interfaces;
using EventBookingService.Domain.Entities;
using EventBookingService.Infrastructure.Context;
using EventBookingService.Infrastructure.Entities;
using EventBookingService.Infrastructure.Mapping;
using EventBookingService.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

namespace EventBookingService.Infrastructure.Repositories;

/// <summary>
/// Репозиторий бля бронирования
/// </summary>
/// <param name="factory"></param>
public class BookingRepository(IDbContextFactory<AppDbContext> factory) : IBookingRepository
{
    ///<inheritdoc/>
    public async Task AddAsync(Booking booking, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        var entity = booking.ToEntity();
        await context.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }

    ///<inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var affectedRows = await context
            .Bookings
            .Where(b => b.Id == id)
            .ExecuteDeleteAsync(ct);

        return affectedRows > 0;
    }

    ///<inheritdoc/>
    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var entity = await context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return entity?.ToDomain();
    }

    ///<inheritdoc/>
    public async Task<List<Booking>> GetAllAsync(BookingStatus status, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var entities = await context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == status.ToString())
            .ToListAsync(ct);

        return entities
            .Select(e => e.ToDomain())
            .ToList();
    }

    //<inheritdoc/>
    [Obsolete("Используйте UpdateInContextAsync для работы в транзакциях")]
    public async Task UpdateAsync(Booking booking, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        var entity = await context.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id, ct);

        if (entity != null)
        {
            booking.UpdateEntity(entity);
            await context.SaveChangesAsync(ct);
        }
    }


    ///<inheritdoc/>
    public async Task AddInContextAsync(Booking booking, ITransactionContext context, CancellationToken ct)
    {
        var appDbContext = context as TransactionContext
            ?? throw new ArgumentException($"Context must be of type {nameof(AppDbContext)}", nameof(context));

        var entity = booking.ToEntity();
        await appDbContext.DbContext.AddAsync(entity, ct);
    }

    public async Task<List<Booking>> GetUserBookingInContextAsync(Guid userId, ITransactionContext context, CancellationToken ct)
    {
        var tx = context as TransactionContext
                 ?? throw new ArgumentException($"Context must be {nameof(TransactionContext)}", nameof(context));

        return await tx.DbContext.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId &&
                        (b.Status == nameof(BookingStatus.Pending) || b.Status == nameof(BookingStatus.Confirmed)))
            .Select(e => e.ToDomain())
            .ToListAsync(ct);
    }

    ///<inheritdoc/>
    public async Task UpdateInContextAsync(Booking booking, ITransactionContext context, CancellationToken ct)
    {
        var appDbContext = context as TransactionContext 
            ?? throw new ArgumentException($"Context must be of type {nameof(TransactionContext)}", nameof(context));

        // Ищем уже загруженную сущность в контексте
        var trackedEntity = appDbContext.DbContext.ChangeTracker
            .Entries<BookingEntity>()
            .FirstOrDefault(e => e.Entity.Id == booking.Id)
            ?.Entity;

        // Если сущность не загружена в контекст, загружаем её
        if (trackedEntity == null)
        {
            trackedEntity = await appDbContext.DbContext.Bookings
                .FirstOrDefaultAsync(b => b.Id == booking.Id, ct);
        }

        if (trackedEntity != null)
        {
            // Обновляем только те свойства, которые могли измениться в доменной модели
            booking.UpdateEntity(trackedEntity);
        }
    }
}
