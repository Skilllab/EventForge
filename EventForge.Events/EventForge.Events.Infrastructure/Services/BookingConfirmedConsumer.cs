using System.Text.Json;

using Confluent.Kafka;

using EventForge.Contract.Brokers;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Infrastructure.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Events.Infrastructure.Services;

/// <summary>
/// Kafka consumer события BookingConfirmed.
/// Обработка идемпотентна по MessageId.
/// </summary>
public class BookingConfirmedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<BookingConfirmedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            GroupId = kafkaOptions.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TopicNames.BookingConfirmed);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null)
                {
                    continue;
                }

                var message = JsonSerializer.Deserialize<BookingConfirmed>(consumeResult.Message.Value);
                if (message == null)
                {
                    logger.LogWarning("Получено пустое или невалидное сообщение BookingConfirmed");
                    continue;
                }

                await using var scope = scopeFactory.CreateAsyncScope();

                var processedRepository = scope.ServiceProvider.GetRequiredService<IProcessedMessageRepository>();
                var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

                // Идемпотентность: если уже обрабатывали этот MessageId, просто коммитим и идем дальше.
                if (await processedRepository.ExistsAsync(message.MessageId, stoppingToken))
                {
                    logger.LogInformation("Сообщение {MessageId} уже обработано, пропускаем", message.MessageId);
                    consumer.Commit(consumeResult);
                    continue;
                }

                var reserved = await eventService.TryReserveSeatAsync(message.EventId, stoppingToken);
                if (!reserved)
                {
                    logger.LogWarning("Не удалось уменьшить места для EventId={EventId}. Нет свободных мест.", message.EventId);
                    consumer.Commit(consumeResult);
                    continue;
                }

                await processedRepository.AddAsync(message.MessageId, nameof(BookingConfirmed), stoppingToken);

                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Не падаем на одном плохом сообщении.
                logger.LogError(ex, "Ошибка при обработке сообщения BookingConfirmed");
            }
        }
    }
}
