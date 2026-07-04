namespace EventForge.Contract.Brokers;

/// <summary>
/// Контракт события отмененного бронирования
/// </summary>
public sealed class BookingCancelled
{
    public Guid MessageId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public int SeatsCount { get; private set; }
    public DateTime CancelledAt { get; private set; }

    private BookingCancelled(Guid messageId, Guid bookingId, Guid eventId, Guid userId, int seatsCount, DateTime cancelledAt)
    {
        MessageId = messageId;
        BookingId = bookingId;
        EventId = eventId;
        UserId = userId;
        SeatsCount = seatsCount;
        CancelledAt = cancelledAt;
    }

    public static BookingCancelled Create(Guid messageId, Guid bookingId, Guid eventId, Guid userId, int seatsCount, DateTime cancelledAt) =>
        new(messageId, bookingId, eventId, userId, seatsCount, cancelledAt);
}
