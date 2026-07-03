using EventForge.Booking.Application.DTO;

namespace EventForge.Booking.Application.Interfaces;

/// <summary>
/// Контракт взаимодействия Booking-сервиса с Events-сервисом.
/// </summary>
public interface IEventsGateway
{
    /// <summary>
    /// Получить состояние события.
    /// </summary>
    Task<EventStateDTO?> GetEventAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Попытаться зарезервировать одно место.
    /// </summary>
    Task<bool> TryReserveSeatAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Освободить одно место.
    /// </summary>
    Task ReleaseSeatAsync(Guid eventId, CancellationToken ct);
}
