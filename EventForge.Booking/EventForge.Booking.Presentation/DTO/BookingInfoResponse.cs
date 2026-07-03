namespace EventForge.Booking.Presentation.DTO;

/// <summary>
/// DTO для ответа по созданию бронирования
/// </summary>
/// <param name="ID">Идентификатор бронирования</param>
/// <param name="EventID">Идентификатор события, по которому создано бронирование</param>
/// <param name="Status">Статус бронирования</param>
public record BookingInfoResponse(
    Guid ID,
    Guid EventID,
    string Status
);