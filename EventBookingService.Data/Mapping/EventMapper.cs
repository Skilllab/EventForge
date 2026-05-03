using EventBookingService.Data.Entities;
using EventBookingService.Domain.Entities;

namespace EventBookingService.Data.Mapping;

public static class EventMapper
{
    public static Event ToDomain(this EventEntity entity)
    {
        // Используем статический метод создания или рефлексию 
        // для установки полей, которые нельзя менять напрямую
        var domain = Event.Create(
            entity.Title,
            entity.StartAt,
            entity.EndAt,
            entity.TotalSeats,
            entity.Description);

        // Чтобы синхронизировать Id и остаток мест из БД:
        typeof(Event).GetProperty(nameof(Event.Id))?.SetValue(domain, entity.Id);
        typeof(Event).GetProperty(nameof(Event.AvailableSeats))?.SetValue(domain, entity.AvailableSeats);

        return domain;
    }

    public static EventEntity ToEntity(this Event domain)
    {
        return new EventEntity
        {
            Id = domain.Id,
            Title = domain.Title,
            Description = domain.Description,
            StartAt = domain.StartAt,
            EndAt = domain.EndAt,
            TotalSeats = domain.TotalSeats,
            AvailableSeats = domain.AvailableSeats,
            Bookings = new List<BookingEntity>()
        };
    }
}
