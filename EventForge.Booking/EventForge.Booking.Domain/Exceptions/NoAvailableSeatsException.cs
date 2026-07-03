namespace EventForge.Booking.Domain.Exceptions;

/// <summary>
/// Исключение, которое выбрасывается в случае если у события нет мест для бронирования
/// </summary>
public class NoAvailableSeatsException : DomainException
{
    /// <inheritdoc />
    public NoAvailableSeatsException(string entityName, string entityId)
        : base($"Для элемента {entityName} c ID: {entityId} нет доступных мест для бронирования", entityName, entityId) { }

    /// <inheritdoc />
    public NoAvailableSeatsException(string entityName, string entityId, string message)
        : base(message, entityName, entityId) { }
}
