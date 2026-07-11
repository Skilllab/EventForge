namespace EventForge.Contract.Enums;

/// <summary>
/// Причины отклонения бронирования
/// </summary>
public enum  BookingNotApprovedReason
{
    /// <summary>
    /// Событие не найдено
    /// </summary>
    EventNotFound = 0,

    /// <summary>
    /// Событие уже началось
    /// </summary>
    EventStarted = 1,

    /// <summary>
    /// Нет доступных мест
    /// </summary>
    NoSeats = 2,
}
