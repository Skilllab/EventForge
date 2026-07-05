namespace EventForge.Contract.Brokers;

/// <summary>
/// Имена Kafka-топиков, общие для издателей и подписчиков
/// </summary>
public static class TopicNames
{
    /// <summary>
    /// Топик подтвержденных бронирований
    /// </summary>
    public const string BookingConfirmed = "booking-confirmed";

    /// <summary>
    /// Топик отклоненных бронирований
    /// </summary>
    public const string BookingRejected = "booking-rejected";

    /// <summary>
    /// Топик отмененных бронирований
    /// </summary>
    public const string BookingCancelled = "booking-cancelled";
}
