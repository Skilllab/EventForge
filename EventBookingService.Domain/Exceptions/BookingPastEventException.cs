namespace EventBookingService.Domain.Exceptions;

/// <summary>
/// Исключение, которое выбрасывается при бронировании на прошедшее событие
/// </summary>
public class BookingPastEventException : DomainException
{
    //<inheritdoc />
    public BookingPastEventException(string entityName, string entityId)
        : base($"Нельзя забронировать прошедшее событие '{entityName}' с ID: '{entityId}'.", entityName, entityId) { }

    // <inheritdoc />
    public BookingPastEventException(string entityName, string entityId, string message)
        : base(message, entityName, entityId) { }
}
