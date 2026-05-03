using EventBookingService.Domain.Entities;

namespace EventBookingService.Domain.Interfaces;

/// <summary>
/// Основной интерфейс репозитория бронирования
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Добавление бронирования
    /// </summary>
    /// <param name="booking">Доменная модель бронирование</param>
    /// <param name="ct">Токен отмены</param>
    Task AddAsync(Booking booking, CancellationToken ct);

    /// <summary>
    /// Удаление бронирования
    /// </summary>
    /// <param name="id">ID бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Получение бронирования по ID
    /// </summary>
    /// <param name="id">ID бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct);


    /// <summary>
    /// Получение всех бронирований с фильтрацией
    /// </summary>
    /// <param name="status">Статус бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task<List<Booking>> GetAll(BookingStatus status, CancellationToken ct);

    /// <summary>
    /// Обновление бронирования
    /// </summary>
    /// <param name="booking">Доменная модель бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task UpdateAsync(Booking booking, CancellationToken ct);
}
