using System.ComponentModel;

namespace EventBookingService.WebAPI.Models.Domain;

/// <summary>
/// Статус бронирования
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// В процессе
    /// </summary>
    [Description("В процессе")]
    Pending = 0,

    /// <summary>
    /// Бронь подтверждена
    /// </summary>
    [Description("Бронь подтверждена")]
    Confirmed = 1,

    /// <summary>
    /// Бронь отменена
    /// </summary>
    [Description("Бронь отменена")]
    Rejected = 2,
}
