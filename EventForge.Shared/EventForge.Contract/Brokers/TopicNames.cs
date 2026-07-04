namespace EventForge.Contract.Brokers;

/// <summary>
/// Имена Kafka-топиков, общие для издателей и подписчиков
/// </summary>
public static class TopicNames
{
    /// <summary>
    /// Топик подтвержденных бронирований.
    /// </summary>
    public const string BookingConfirmed = "booking-confirmed";
}
