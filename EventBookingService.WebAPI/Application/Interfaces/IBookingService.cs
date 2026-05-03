using EventBookingService.WebAPI.Models.DTO.Booking;

namespace EventBookingService.WebAPI.Application.Interfaces;

/// <summary>
/// Интерфейс сервиса бронирования
/// </summary>
public interface IBookingService
{
    /// <summary>
    ///  Создание брони для указанного события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<BookingInfoDTO> CreateBookingAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Обновить все бронирования со статусом Pending в репозитории
    /// </summary>
    /// <param name="ct">Токен отмены</param>
    Task UpdateBookingAsync(CancellationToken ct);

    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId">ID бронирования</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns></returns>
    Task<BookingInfoDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct);
}