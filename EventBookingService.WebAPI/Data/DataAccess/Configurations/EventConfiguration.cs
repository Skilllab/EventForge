using EventBookingService.WebAPI.Models.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventBookingService.WebAPI.Data.DataAccess.Configurations;

/// <summary>
/// Конфигурация EF для <see cref="Event"/>
/// </summary>
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Event> builder)
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
