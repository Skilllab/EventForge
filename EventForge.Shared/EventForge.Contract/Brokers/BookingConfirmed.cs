using System;

namespace EventForge.Contract.Brokers;

/// <summary>
/// Контракт события подтвержденного бронирования.
/// Содержит только данные, нужные подписчику.
/// </summary>
/// <param name="messageId">Уникальный идентификатор сообщения для идемпотентной обработки</param>
/// <param name="bookingId">Идентификатор бронирования</param>
/// <param name="eventId">Идентификатор события</param>
/// <param name="userId">Идентификатор пользователя</param>
/// <param name="seatsCount">Количество подтвержденных мест</param>
/// <param name="confirmedAt">Время подтверждения</param>
public sealed class BookingConfirmed(
    Guid messageId,
    Guid bookingId,
    Guid eventId,
    Guid userId,
    int seatsCount,
    DateTime confirmedAt)
{
    /// <summary>
    /// Уникальный идентификатор сообщения для идемпотентной обработки
    /// </summary>
    public Guid MessageId{ get; } = messageId;

    /// <summary>
    /// Идентификатор бронирования
    /// </summary>
    public Guid BookingId{ get; } = bookingId;

    /// <summary>
    /// Идентификатор события
    /// </summary>
    public Guid EventId{ get; } = eventId;

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public Guid UserId{ get; } = userId;

    /// <summary>
    /// Количество подтвержденных мест
    /// </summary>
    public int SeatsCount{ get; } = seatsCount;

    /// <summary>
    /// Время подтверждения
    /// </summary>
    public DateTime ConfirmedAt{ get; } = confirmedAt;
}
