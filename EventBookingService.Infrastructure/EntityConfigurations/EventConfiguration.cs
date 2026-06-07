using EventBookingService.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventBookingService.Infrastructure.EntityConfigurations;

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
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(p => p.StartAt)
            .HasColumnName("start_at")
            .IsRequired();

        builder.Property(p => p.EndAt)
            .HasColumnName("end_at")
            .IsRequired();

        builder.Property(p => p.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(p => p.AvailableSeats)
            .HasColumnName("available_seats")
            .IsRequired();

        builder.HasMany(p => p.Bookings)
            .WithOne()
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Title);
        builder.HasIndex(x => x.StartAt);
        builder.HasIndex(x => x.EndAt);
    }
}
