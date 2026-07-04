using System;

namespace EventForge.Contract.Brokers;

/// <summary>
/// Контракт события подтвержденного бронирования
/// </summary>
public sealed class BookingConfirmed
{
    /// <summary>
    /// Уникальный идентификатор сообщения для идемпотентной обработки
    /// </summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// Идентификатор бронирования
    /// </summary>
    public Guid BookingId { get; private set; }

    /// <summary>
    /// Идентификатор события
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Количество подтвержденных мест
    /// </summary>
    public int SeatsCount { get; private set; }

    /// <summary>
    /// Время подтверждения
    /// </summary>
    public DateTime ConfirmedAt { get; private set; }

    private BookingConfirmed(Guid messageId, Guid bookingId, Guid bookingEventId, Guid bookingUserId, int seatsCount, DateTime processingTime)
    {
        MessageId = messageId;
        BookingId = bookingId;
        EventId = bookingEventId;
        UserId = bookingUserId;
        SeatsCount = seatsCount;
        ConfirmedAt = processingTime;
    }

    /// <summary>
    /// Создает новый экземпляр события подтвержденного бронирования
    /// </summary>
    /// <param name="messageId">Уникальный идентификатор сообщения для идемпотентной обработки</param>
    /// <param name="bookingId">Идентификатор бронирования</param>
    /// <param name="bookingEventId">Идентификатор события</param>
    /// <param name="bookingUserId">Идентификатор пользователя</param>
    /// <param name="seatsCount">Количество подтвержденных мест</param>
    /// <param name="processingTime">Время подтверждения</param>
    /// <returns></returns>
    public static BookingConfirmed Create(Guid messageId, Guid bookingId, Guid bookingEventId, Guid bookingUserId, int seatsCount, DateTime processingTime) =>
        new(
            messageId,
            bookingId,
            bookingEventId,
            bookingUserId,
            seatsCount,
            processingTime
        );
}
