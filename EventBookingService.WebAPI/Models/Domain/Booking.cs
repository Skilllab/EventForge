using System.ComponentModel.DataAnnotations;

namespace EventBookingService.WebAPI.Models.Domain;

/// <summary>
/// Модель бронирования события
/// </summary>
public class Booking
{
    /// <summary>
    /// Уникальный идентификатор брони
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Текущий статус брони
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Дата и время обработки брони
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    private Booking(Guid eventId, DateTime createdAt)
    {
        Id = Guid.NewGuid();
        Status = BookingStatus.Pending;
        CreatedAt = createdAt;
        EventId = eventId;
    }

    /// <summary>
    /// Метод создания события
    /// </summary>
    /// <param name="eventId">Идентификатор привязанного события</param>
    /// <param name="createdAt">Время создания брони. Может быть разное в зависимости от региона</param>
    /// <returns></returns>
    public static Booking Create(Guid eventId, DateTime createdAt) => new(eventId, createdAt);
}
