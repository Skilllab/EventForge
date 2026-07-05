using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Context;
using EventForge.Booking.Infrastructure.Mapping;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Booking.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для бронирования
/// </summary>
public class BookingRepository(IDbContextFactory<BookingDbContext> factory) : IBookingRepository
{
    ///<inheritdoc/>
    public async Task AddAsync(BookingModel booking, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        await context.AddAsync(booking.ToEntity(), ct);
        await context.SaveChangesAsync(ct);
    }

    ///<inheritdoc/>
    public async Task<BookingModel?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var entity = await context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return entity?.ToDomain();
    }

    ///<inheritdoc/>
    public async Task<List<BookingModel>> GetAllAsync(BookingStatus status, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var entities = await context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == status.ToString())
            .ToListAsync(ct);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    ///<inheritdoc/>
    public async Task<int> GetUserActiveBookingsCountAsync(Guid userId, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        return await context.Bookings.CountAsync(
            b => b.UserId == userId &&
                 (b.Status == nameof(BookingStatus.Pending) || b.Status == nameof(BookingStatus.Confirmed)),
            ct);
    }

    ///<inheritdoc/>
    public async Task<bool> ConfirmAndAddOutboxAsync(
        Guid bookingId,
        DateTime processedAt,
        OutboxMessage outboxMessage,
        CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var entity = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        if (entity == null)
            return false;

        if (entity.Status != nameof(BookingStatus.Pending))
            return false;

        var booking = entity.ToDomain();
        booking.Confirm(processedAt);
        booking.UpdateEntity(entity);

        await context.OutboxMessages.AddAsync(outboxMessage.ToEntity(), ct);
        await context.SaveChangesAsync(ct);

        return true;
    }

    ///<inheritdoc/>
    public async Task<bool> CancelAndAddOutboxAsync(
        Guid bookingId,
        Guid userId,
        DateTime processedAt,
        OutboxMessage outboxMessage,
        CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var entity = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        if (entity == null)
            return false;

        if (entity.UserId != userId)
            return false;

        if (entity.Status == nameof(BookingStatus.Cancelled) || entity.Status == nameof(BookingStatus.Rejected))
            return false;

        var booking = entity.ToDomain();
        booking.Cancel(processedAt);
        booking.UpdateEntity(entity);

        await context.OutboxMessages.AddAsync(outboxMessage.ToEntity(), ct);
        await context.SaveChangesAsync(ct);

        return true;
    }

    ///<inheritdoc/>
    public async Task<bool> RejectAndAddOutboxAsync(
        Guid bookingId,
        DateTime processedAt,
        OutboxMessage outboxMessage,
        CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var entity = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        if (entity == null)
            return false;

        if (entity.Status != nameof(BookingStatus.Pending))
            return false;

        var booking = entity.ToDomain();
        booking.Reject(processedAt);
        booking.UpdateEntity(entity);

        await context.OutboxMessages.AddAsync(outboxMessage.ToEntity(), ct);
        await context.SaveChangesAsync(ct);

        return true;
    }
}
