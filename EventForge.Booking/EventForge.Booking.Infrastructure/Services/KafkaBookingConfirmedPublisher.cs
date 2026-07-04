using System.Text.Json;

using Confluent.Kafka;

using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Infrastructure.Common;
using EventForge.Contract.Brokers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Booking.Infrastructure.Services;

/// <summary>
/// Kafka publisher события BookingConfirmed
/// </summary>
public sealed class KafkaBookingConfirmedPublisher : IBookingConfirmedPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaBookingConfirmedPublisher> _logger;

    public KafkaBookingConfirmedPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaBookingConfirmedPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    //<inheritdoc />
    public async Task PublishAsync(BookingConfirmed message, CancellationToken ct)
    {
        // Ключ = EventId, чтобы сообщения по одному событию шли последовательно.
        var payload = JsonSerializer.Serialize(message);
        await PublishRawAsync(TopicNames.BookingConfirmed, message.EventId.ToString(), payload, ct);
    }

    //<inheritdoc />
    public async Task PublishRawAsync(string topic, string key, string payload, CancellationToken ct)
    {
        await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = payload
            },
            ct);

        _logger.LogInformation("Сообщение опубликовано в Kafka. Topic={Topic}, Key={Key}", topic, key);
    }

    /// <summary>
    /// Диспоузим продюсер
    /// </summary>
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
