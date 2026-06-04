using EventBookingService.Domain.Entities;
using EventBookingService.Infrastructure.Entities;

namespace EventBookingService.Infrastructure.Mapping;

/// <summary>
/// Маппер между доменной моделью и сущностью базы данных
/// </summary>
public static class BookingMapper
{
    /// <summary>
    /// Из сущности БД в доменную модель
    /// </summary>
    public static Booking ToDomain(this BookingEntity entity)
    {
        var domain = Booking.Create(entity.EventId, entity.CreatedAt);

        //Восстанавливаем состояние, которое закрыто для изменений извне
        var type = typeof(Booking);
        type.GetProperty(nameof(Booking.Id))?.SetValue(domain, entity.Id);

        domain.Status = Enum.Parse<BookingStatus>(entity.Status);
        domain.ProcessedAt = entity.ProcessedAt;

        return domain;
    }

    /// <summary>
    /// Из домена в сущность БД
    /// </summary>
    public static BookingEntity ToEntity(this Booking domain) =>
        new()
        {
            Id = domain.Id,
            EventId = domain.EventId,
            Status = domain.Status.ToString(),
            CreatedAt = domain.CreatedAt,
            ProcessedAt = domain.ProcessedAt
        };

    public static void UpdateEntity(this Booking domain, BookingEntity entity)
    {
        entity.Status = domain.Status.ToString();
        entity.ProcessedAt = domain.ProcessedAt;
    }
}
