using System.Text.Json;

using Confluent.Kafka;

using EventForge.Contract.Brokers;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Domain.Exceptions;
using EventForge.Events.Infrastructure.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Events.Infrastructure.Services;

/// <summary>
/// Consumer события BookingCancelled.
/// Освобождает место в Events.
/// </summary>
public class BookingCancelledConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<BookingCancelledConsumer> logger) : BackgroundService
{
    public async Task HandleMessageAsync(BookingCancelled? message, CancellationToken stoppingToken)
    {
        if (message == null)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var processedRepository = scope.ServiceProvider.GetRequiredService<IProcessedMessageRepository>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        if (await processedRepository.ExistsAsync(message.MessageId, stoppingToken))
        {
            return;
        }

        try
        {
            await eventService.ReleaseSeatAsync(message.EventId, stoppingToken);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex,
                "Событие {EventId} не найдено для сообщения {MessageId}. Сообщение будет помечено обработанным.",
                message.EventId,
                message.MessageId);
        }

        await processedRepository.AddAsync(message.MessageId, nameof(BookingCancelled), stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            GroupId = kafkaOptions.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TopicNames.BookingCancelled);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null)
                {
                    continue;
                }

                var message = JsonSerializer.Deserialize<BookingCancelled>(consumeResult.Message.Value);
                await HandleMessageAsync(message, stoppingToken);
                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке сообщения BookingCancelled");
            }
        }
    }
}
