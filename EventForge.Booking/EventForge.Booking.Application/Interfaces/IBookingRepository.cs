using EventForge.Booking.Domain.Entities;


namespace EventForge.Booking.Application.Interfaces;

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
    Task AddAsync(BookingModel booking, CancellationToken ct);

    /// <summary>
    /// Получение бронирования по ID
    /// </summary>
    /// <param name="id">ID бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task<BookingModel?> GetByIdAsync(Guid id, CancellationToken ct);


    /// <summary>
    /// Получение всех бронирований с фильтрацией
    /// </summary>
    /// <param name="status">Статус бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task<List<BookingModel>> GetAllAsync(BookingStatus status, CancellationToken ct);

    /// <summary>
    /// Обновление бронирования
    /// </summary>
    /// <param name="booking">Доменная модель бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task UpdateAsync(BookingModel booking, CancellationToken ct);


    /// <summary>
    /// Получение бронирования пользователя с блокировкой FOR UPDATE внутри транзакции
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Количество активных бронирований пользователя</returns>
    Task<int> GetUserActiveBookingsCountAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Атомарно подтверждает бронь и добавляет интеграционное событие в outbox.
    /// </summary>
    /// <param name="bookingId">ID бронирования</param>
    /// <param name="processedAt">Время обработки бронирования</param>
    /// <param name="outboxMessage">Сообщение для добавления в outbox</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> ConfirmAndAddOutboxAsync(Guid bookingId, DateTime processedAt, OutboxMessage outboxMessage, CancellationToken ct);

}
