namespace EventForge.Booking.Domain.Entities;

/// <summary>
/// Запись outbox для гарантированной последующей публикации сообщения.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Уникальный идентификатор outbox-сообщения
    /// </summary>
    public Guid Id{ get; set; }

    /// <summary>
    /// Тип сообщения
    /// </summary>
    public required string Type{ get; set; }

    /// <summary>
    /// Имя топика Kafka
    /// </summary>
    public required string Topic{ get; set; }

    /// <summary>
    /// Ключ сообщения Kafka
    /// </summary>
    public required string MessageKey{ get; set; }

    /// <summary>
    /// JSON-представление события
    /// </summary>
    public required string Payload{ get; set; }

    /// <summary>
    /// Время создания записи
    /// </summary>
    public DateTime CreatedAt{ get; set; }

    /// <summary>
    /// Время успешной публикации
    /// </summary>
    public DateTime? ProcessedAt{ get; set; }

    /// <summary>
    /// Последняя ошибка публикации
    /// </summary>
    public string? Error{ get; set; }

    /// <summary>
    /// Создает новый экземпляр OutboxMessage
    /// </summary>
    /// <param name="type">Тип сообщения</param>
    /// <param name="topic">Имя топика Kafka</param>
    /// <param name="messageKey">Ключ сообщения Kafka</param>
    /// <param name="payload">JSON-представление события</param>
    /// <param name="createdAt">Время создания записи</param>
    /// <param name="error">Последняя ошибка публикации</param>
    /// <returns>Новый экземпляр OutboxMessage</returns>
    public static OutboxMessage Create(string type, string topic, string messageKey, string payload, DateTime createdAt, string? error)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Topic = topic,
            MessageKey = messageKey,
            Payload = payload,
            CreatedAt = createdAt,
            Error = error
        };
    }
}
