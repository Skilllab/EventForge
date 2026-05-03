namespace EventBookingService.WebAPI.Models.DTO.Booking;

/// <summary>
/// DTO класс для ответа по созданию бронирования
/// </summary>
public class BookingInfoDTO
{
    /// <summary>
    /// Идентификатор бронирования
    /// </summary>
    public Guid ID { get; init; }

    /// <summary>
    /// Идентификатор события по которому создано бронирование
    /// </summary>
    public Guid EventID { get; init; }

    /// <summary>
    /// Статус бронирования
    /// </summary>
    public required string Status { get; init; }
}
