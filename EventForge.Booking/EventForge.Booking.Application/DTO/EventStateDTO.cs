namespace EventForge.Booking.Application.DTO;

/// <summary>
/// Снимок состояния события для Booking-сервиса.
/// </summary>
public sealed record EventStateDTO(
    Guid Id,
    DateTime StartAt,
    int AvailableSeats);
