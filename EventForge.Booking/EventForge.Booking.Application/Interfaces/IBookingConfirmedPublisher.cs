using EventForge.Contract.Brokers;

namespace EventForge.Booking.Application.Interfaces;

/// <summary>
/// Контракт публикации события подтвержденной брони.
/// </summary>
public interface IBookingConfirmedPublisher
{
    /// <summary>
    /// Публикует типизированное событие
    /// </summary>
    Task PublishAsync(BookingConfirmed message, CancellationToken ct);

    /// <summary>
    /// Публикует сериализованный payload
    /// </summary>
    Task PublishRawAsync(string topic, string key, string payload, CancellationToken ct);
}
