using EventForge.Booking.Domain.Entities;


namespace EventForge.Booking.Application.Interfaces;

/// <summary>
/// Основной интерфейс репозитория бронирования
/// </summary>
public interface IBookingRepository
{
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
    /// Получение всех бронирований
    /// </summary>
    /// <param name="ct">Токен отмены</param>
    Task<List<BookingModel>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Получение бронирования пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Количество активных бронирований пользователя</returns>
    Task<int> GetUserActiveBookingsCountAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Метод создает бронирование и добавляет интеграционное событие в outbox
    /// </summary>
    /// <param name="bookingId"></param>
    /// <param name="processedAt"></param>
    /// <param name="outboxMessage"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> CreateAndAddOutboxAsync(BookingModel booking, OutboxMessage outboxMessage, CancellationToken ct);

    /// <summary>
    /// Метод отменяет бронь и добавляет интеграционное событие в outbox
    /// </summary>
    /// <param name="bookingId">ID бронирования</param>
    /// <param name="userId">ID пользователя</param>
    /// <param name="processedAt">Время обработки бронирования</param>
    /// <param name="outboxMessage">Сообщение для добавления в outbox</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> CancelAndAddOutboxAsync(Guid bookingId, Guid userId, DateTime processedAt, OutboxMessage outboxMessage, CancellationToken ct);

    /// <summary>
    /// Подтверждает бронирование: переводит из Pending в Confirmed.
    /// БЕЗ создания outbox-сообщения — используется, когда outbox уже опубликован
    /// другим сервисом (Events).
    /// </summary>
    /// <param name="bookingId">ID бронирования.</param>
    /// <param name="processedAt">Время обработки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>true — подтверждено; false — бронь не найдена или не в статусе Pending.</returns>
    Task<bool> ConfirmBookingAsync(Guid bookingId, DateTime processedAt, CancellationToken ct);

    /// <summary>
    /// Отклоняет бронирование: переводит из Pending в Rejected.
    /// БЕЗ создания outbox-сообщения — используется, когда outbox уже опубликован
    /// другим сервисом (Events).
    /// </summary>
    /// <param name="bookingId">ID бронирования.</param>
    /// <param name="processedAt">Время обработки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>true — отклонено; false — бронь не найдена или не в статусе Pending.</returns>
    Task<bool> RejectBookingAsync(Guid bookingId, DateTime processedAt, CancellationToken ct);
}
