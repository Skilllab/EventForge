using EventBookingService.WebAPI.Models.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventBookingService.WebAPI.Data.DataAccess.Configurations
{
    /// <summary>
    /// Конфигурация EF для <see cref="Booking"/>
    /// </summary>
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings");

            //Первичный ключ
            builder.HasKey(k => k.Id);
            //Отключаем генерацию в коде
            builder.Property(k => k.Id)
                .ValueGeneratedNever();

            builder.Property(p => p.Status)
                .HasConversion<BookingStatus>()
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.ProcessedAt);

            builder.HasOne<Event>()
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.EventId);
        }
    }

}
