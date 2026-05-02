using EventBookingService.Data.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventBookingService.Data.Context.Configurations
{
    /// <summary>
    /// Конфигурация EF для <see cref="BookingEntity"/>
    /// </summary>
    public class BookingConfiguration : IEntityTypeConfiguration<BookingEntity>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<BookingEntity> builder)
        {
            builder.ToTable("Bookings");

            //Первичный ключ
            builder.HasKey(k => k.Id);
            //Отключаем генерацию в коде
            builder.Property(k => k.Id)
                .ValueGeneratedNever();

            builder.Property(p => p.Status)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.ProcessedAt);

            builder.HasOne<EventEntity>()
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.EventId);
        }
    }

}
