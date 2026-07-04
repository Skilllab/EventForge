namespace EventForge.Contract.Brokers;

/// <summary>
/// Контракт события отклоненного бронирования.
/// </summary>
/// <param name="MessageId">Идентификатор сообщения.</param>
/// <param name="BookingId">Идентификатор бронирования.</param>
/// <param name="EventId">Идентификатор события (мероприятия).</param>
/// <param name="UserId">Идентификатор пользователя.</param>
/// <param name="SeatsCount">Количество отклоненных мест.</param>
/// <param name="RejectedAt">Дата и время отклонения бронирования.</param>
public sealed record BookingRejected(
    Guid MessageId,
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime RejectedAt);
