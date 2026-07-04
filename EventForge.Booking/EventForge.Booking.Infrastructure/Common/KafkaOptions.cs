namespace EventForge.Booking.Infrastructure.Common;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class KafkaOptions
{
    /// <summary>
    /// Адрес Kafka bootstrap servers
    /// </summary>
    public string BootstrapServers { get; set; } = string.Empty;
}
