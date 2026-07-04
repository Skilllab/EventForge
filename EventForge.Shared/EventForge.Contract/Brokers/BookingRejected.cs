namespace EventForge.Contract.Brokers;

/// <summary>
/// Контракт события отклоненного бронирования
/// </summary>
public sealed class BookingRejected
{
    public Guid MessageId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public int SeatsCount { get; private set; }
    public DateTime RejectedAt { get; private set; }

    private BookingRejected(Guid messageId, Guid bookingId, Guid eventId, Guid userId, int seatsCount, DateTime rejectedAt)
    {
        MessageId = messageId;
        BookingId = bookingId;
        EventId = eventId;
        UserId = userId;
        SeatsCount = seatsCount;
        RejectedAt = rejectedAt;
    }

    public static BookingRejected Create(Guid messageId, Guid bookingId, Guid eventId, Guid userId, int seatsCount, DateTime rejectedAt) =>
        new(messageId, bookingId, eventId, userId, seatsCount, rejectedAt);
}
