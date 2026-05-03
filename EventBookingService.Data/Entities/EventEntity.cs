namespace EventBookingService.Data.Entities;

/// <summary>
/// Сущность события для БД
/// </summary>
public class EventEntity
{
    /// <summary>
    /// Уникальный идентификатор события
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Название события
    /// </summary>
    public string Title { get; set; } = null!;
    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime StartAt { get; set; }
    /// <summary>
    /// Дата завершения события
    /// </summary>
    public DateTime EndAt { get; set; }
    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    public int TotalSeats { get; set; }
    /// <summary>
    /// Текущее количество свободных мест
    /// </summary>
    public int AvailableSeats { get; set; }

    /// <summary>
    /// Свойство навигации для бронирования
    /// </summary>
    public List<BookingEntity> Bookings { get; set; } = new();
}
