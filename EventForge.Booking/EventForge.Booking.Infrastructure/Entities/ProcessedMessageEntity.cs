namespace EventForge.Booking.Infrastructure.Entities;

/// <summary>
/// Запись о сообщении, которое уже было обработано consumer-ом.
/// </summary>
public class ProcessedMessageEntity
{
    /// <summary>
    /// Идентификатор сообщения (совпадает с MessageId из Kafka-контракта).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Тип обработанного сообщения (например, BookingConfirmed, BookingRejected).
    /// </summary>
    public required string MessageType { get; set; }

    /// <summary>
    /// Дата и время обработки.
    /// </summary>
    public DateTime ProcessedAt { get; set; }
}
