using System.ComponentModel;

namespace EventForge.Contract.Enums;

/// <summary>
/// Причины отклонения бронирования
/// </summary>
public enum  BookingNotApprovedReason
{
    /// <summary>
    /// Событие уже началось
    /// </summary>
    [Description("Событие уже началось")]
    EventStarted = 0,

    /// <summary>
    /// Нет доступных мест
    /// </summary>
    [Description("Нет доступных мест")]
    NoSeats = 1,
}
