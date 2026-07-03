using EventForge.Booking.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Booking.Infrastructure.Context;

/// <summary>
/// DbContext сервиса бронирования
/// </summary>
public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Booking");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
    }
}
