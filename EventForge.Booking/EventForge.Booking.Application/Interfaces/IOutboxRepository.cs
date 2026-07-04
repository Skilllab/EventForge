using EventForge.Booking.Domain.Entities;

namespace EventForge.Booking.Application.Interfaces;

/// <summary>
/// Репозиторий outbox-сообщений
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Добавить outbox-сообщение
    /// </summary>
    /// <param name="message">Сообщение для добавления</param>
    /// <param name="ct">Токен отмены</param>
    Task AddAsync(OutboxMessage message, CancellationToken ct);
    
    /// <summary>
    /// Получить группу необработанных outbox-сообщений
    /// </summary>
    /// <param name="batchSize">Размер группы</param>
    /// <param name="ct">Токен отмены</param>
    Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct);

    /// <summary>
    /// Обновить outbox-сообщение как обработанное
    /// </summary>
    /// <param name="id">Идентификатор сообщения</param>
    /// <param name="ct">Токен отмены</param>
    Task MarkProcessedAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Пометить outbox-сообщение как ошибочное
    /// </summary>
    /// <param name="id"></param>
    /// <param name="error"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct);
}
