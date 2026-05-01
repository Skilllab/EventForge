using EventBookingService.WebAPI.Models.Domain;

using Microsoft.EntityFrameworkCore;

namespace EventBookingService.WebAPI.Data
{
    internal sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<Booking> Bookings => Set<Booking>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Схема для всех таблиц
            modelBuilder.HasDefaultSchema("EventBooking");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
