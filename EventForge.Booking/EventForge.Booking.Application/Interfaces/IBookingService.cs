using EventForge.Booking.Application.DTO;
using EventForge.Shared.Entities.Enums;

namespace EventForge.Booking.Application.Interfaces;

/// <summary>
/// Интерфейс сервиса бронирования
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создание брони для указанного события
    /// </summary>
    Task<BookingInfoDTO> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct);

    /// <summary>
    /// Обновить все бронирования со статусом Pending в репозитории
    /// </summary>
    Task UpdateBookingAsync(CancellationToken ct);

    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    Task<BookingInfoDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct);

    /// <summary>
    /// Отмена бронирования
    /// </summary>
    Task<bool> CancelBooking(Guid bookingId, Guid userId, RoleType userRole, CancellationToken ct);
}
