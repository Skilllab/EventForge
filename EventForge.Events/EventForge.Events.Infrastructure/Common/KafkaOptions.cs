namespace EventForge.Events.Infrastructure.Common;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class KafkaOptions
{
    /// <summary>
    /// Адрес Kafka bootstrap servers
    /// </summary>
    public string BootstrapServers { get; set; } = string.Empty;

    /// <summary>
    /// Имя consumer group
    /// </summary>
    public string ConsumerGroup { get; set; } = string.Empty;
}
