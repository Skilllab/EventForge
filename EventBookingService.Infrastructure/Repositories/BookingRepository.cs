using EventBookingService.Application.Interfaces;
using EventBookingService.Domain.Entities;
using EventBookingService.Infrastructure.Context;
using EventBookingService.Infrastructure.Mapping;

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

    ///<inheritdoc/>
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
    public async Task AddInContextAsync(Booking booking, object context, CancellationToken ct)
    {
        var appDbContext = context as AppDbContext 
            ?? throw new ArgumentException($"Context must be of type {nameof(AppDbContext)}", nameof(context));

        var entity = booking.ToEntity();
        await appDbContext.AddAsync(entity, ct);
    }

    public async Task<List<Booking>> GetUserBooking(Guid userId, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        return context.Bookings.AsNoTracking().Where(b=>b.UserId == userId).Select(e => e.ToDomain()).ToList();
    }
}
