namespace EventForge.Booking.Domain.Entities;

/// <summary>
/// Запись outbox для гарантированной последующей публикации сообщения.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Уникальный идентификатор outbox-сообщения
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Тип сообщения
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Имя топика Kafka
    /// </summary>
    public string Topic { get; private set; }

    /// <summary>
    /// Ключ сообщения Kafka
    /// </summary>
    public string MessageKey { get; private set; }

    /// <summary>
    /// JSON-представление события
    /// </summary>
    public string Payload { get; private set; }

    /// <summary>
    /// Время создания записи
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Время успешной публикации
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Последняя ошибка публикации
    /// </summary>
    public string? Error { get; private set; }

    private OutboxMessage(
        Guid id,
        string type,
        string topic,
        string messageKey,
        string payload,
        DateTime createdAt,
        DateTime? processedAt,
        string? error)
    {
        Id = id;
        Type = type;
        Topic = topic;
        MessageKey = messageKey;
        Payload = payload;
        CreatedAt = createdAt;
        ProcessedAt = processedAt;
        Error = error;
    }

    /// <summary>
    /// Создает новый экземпляр OutboxMessage
    /// </summary>
    public static OutboxMessage Create(
        string type,
        string topic,
        string messageKey,
        string payload,
        DateTime createdAt,
        string? error) =>
        new(
            Guid.NewGuid(),
            type,
            topic,
            messageKey,
            payload,
            createdAt,
            null,
            error);

    /// <summary>
    /// Восстанавливает объект из хранилища.
    /// </summary>
    public static OutboxMessage Restore(
        Guid id,
        string type,
        string topic,
        string messageKey,
        string payload,
        DateTime createdAt,
        DateTime? processedAt,
        string? error) =>
        new(
            id,
            type,
            topic,
            messageKey,
            payload,
            createdAt,
            processedAt,
            error);
}
