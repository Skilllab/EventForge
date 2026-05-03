using EventBookingService.Data.Entities;
using EventBookingService.Data.Interceptors;

using Microsoft.EntityFrameworkCore;

namespace EventBookingService.Data.Context;

public sealed class AppDbContext : DbContext
{
    private readonly LoggingInterceptor _interceptor;

    // Внедряем интерцептор напрямую (Фабрика это умеет)
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        LoggingInterceptor interceptor) : base(options)
    {
        _interceptor = interceptor;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Добавляем его здесь вручную
        optionsBuilder.AddInterceptors(_interceptor);
        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Схема для всех таблиц
        modelBuilder.HasDefaultSchema("EventBooking");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
