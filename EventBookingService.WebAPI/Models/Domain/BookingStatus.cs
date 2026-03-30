namespace EventBookingService.WebAPI.Models.Domain;

/// <summary>
/// Статус бронирования
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// В процессе
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Бронь подтверждена
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Бронь отменена
    /// </summary>
    Rejected = 2,
}
