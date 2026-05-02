using EventBookingService.WebAPI.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;

namespace EventBookingService.WebAPI.Data
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<EventEntity> Events => Set<EventEntity>();
        public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Схема для всех таблиц
            modelBuilder.HasDefaultSchema("EventBooking");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
