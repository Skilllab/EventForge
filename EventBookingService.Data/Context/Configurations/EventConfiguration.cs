using EventBookingService.Data.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventBookingService.Data.Context.Configurations;

/// <summary>
/// Конфигурация EF для <see cref="EventEntity"/>
/// </summary>
public class EventConfiguration : IEntityTypeConfiguration<EventEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder.ToTable("Events");

        //Первичный ключ
        builder.HasKey(k => k.Id);
        //Отключаем генерацию в коде
        builder.Property(k => k.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Title)
            .IsRequired();

        builder.Property(p => p.Description);

        builder.Property(p => p.StartAt);

        builder.Property(p => p.EndAt);

        builder.Property(p => p.TotalSeats);

        builder.Property(p => p.AvailableSeats);

        builder.HasMany(p=>p.Bookings)
            .WithOne()
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
