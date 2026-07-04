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
/// Consumer события BookingRejected.
/// Сейчас используется для аналитики и фиксации факта обработки.
/// Места не освобождает, так как по текущей модели они уменьшаются только на Confirmed.
/// </summary>
public class BookingRejectedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<BookingRejectedConsumer> logger) : BackgroundService
{
    public async Task HandleMessageAsync(BookingRejected? message, CancellationToken stoppingToken)
    {
        if (message == null)
        {
            logger.LogWarning("Получено пустое или невалидное сообщение BookingRejected");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var processedRepository = scope.ServiceProvider.GetRequiredService<IProcessedMessageRepository>();

        if (await processedRepository.ExistsAsync(message.MessageId, stoppingToken))
        {
            logger.LogInformation("Сообщение {MessageId} уже обработано, пропускаем", message.MessageId);
            return;
        }

        logger.LogInformation(
            "Получено аналитическое событие BookingRejected. BookingId={BookingId}, EventId={EventId}",
            message.BookingId,
            message.EventId);

        await processedRepository.AddAsync(message.MessageId, nameof(BookingRejected), stoppingToken);
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
        consumer.Subscribe(TopicNames.BookingRejected);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null)
                {
                    continue;
                }

                var message = JsonSerializer.Deserialize<BookingRejected>(consumeResult.Message.Value);
                await HandleMessageAsync(message, stoppingToken);
                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке сообщения BookingRejected");
            }
        }
    }
}
