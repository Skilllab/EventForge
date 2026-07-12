using System.Text.Json;

using Confluent.Kafka;

using EventForge.Contract.Brokers;
using EventForge.Contract.Enums;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Infrastructure.Entities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Events.Infrastructure.Services;

public class BookingRequestedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<BookingRequestedConsumer> logger) : BackgroundService
{
    public async Task HandleMessageAsync(BookingRequested? message, CancellationToken stoppingToken)
    {
        if (message == null)
        {
            logger.LogWarning("Получено пустое или невалидное сообщение BookingRequested");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var processedRepository = scope.ServiceProvider.GetRequiredService<IProcessedMessageRepository>();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();


        if (await processedRepository.ExistsAsync(message.MessageId, stoppingToken))
        {
            logger.LogInformation("Сообщение {MessageId} уже обработано, пропускаем", message.MessageId);
            return;
        }

        var now = DateTime.UtcNow;
        OutboxMessage outbox;

        var eventEntity = await eventRepository.GetByIdAsync(message.EventId, stoppingToken);
        if (eventEntity == null)
        {
            logger.LogWarning("Событие {EventId} не найдено для сообщения {MessageId}",
                message.EventId, message.MessageId);
            // Стало:
            var rejectedNotFound = new BookingRejected(
                Guid.NewGuid(), message.BookingId, message.EventId,
                message.UserId, now, $"Событие {message.EventId} не найдено для сообщения {message.MessageId}");

            outbox = OutboxMessage.Create(
                nameof(BookingRejected), TopicNames.BookingRejected,
                message.EventId.ToString(),
                JsonSerializer.Serialize(rejectedNotFound), now, null);

            await eventRepository.AddOutboxAsync(outbox, stoppingToken);
            await processedRepository.AddAsync(
                message.MessageId, nameof(BookingRejected), stoppingToken);
            return;
        }

        if (eventEntity.StartAt <= now)
        {
            var rejected = new BookingNotApproved(
                Guid.NewGuid(), message.BookingId, message.EventId,
                message.UserId, now, BookingNotApprovedReason.EventStarted);

            outbox = OutboxMessage.Create(
                nameof(BookingNotApproved), TopicNames.BookingNotApproved,
                message.EventId.ToString(), JsonSerializer.Serialize(rejected), now, null);

            await eventRepository.AddOutboxAsync(outbox, stoppingToken);
            await processedRepository.AddAsync(message.MessageId, nameof(BookingRejected), stoppingToken);
            return;
        }

        var confirmed = new BookingConfirmed(
            Guid.NewGuid(), message.BookingId, message.EventId,
            message.UserId, message.SeatsCount, now);

        outbox = OutboxMessage.Create(
            nameof(BookingConfirmed), TopicNames.BookingConfirmed,
            message.EventId.ToString(), JsonSerializer.Serialize(confirmed), now, null);

        var reserved = await eventRepository.TryReserveSeatAndAddOutboxAsync(
            message.EventId, message.SeatsCount, outbox, stoppingToken);

        if (!reserved)
        {
            logger.LogWarning("Не удалось зарезервировать места для EventId={EventId}", message.EventId);

            var rejected = new BookingNotApproved(
                Guid.NewGuid(), message.BookingId, message.EventId,
                message.UserId, now, BookingNotApprovedReason.NoSeats);

            outbox = OutboxMessage.Create(
                nameof(BookingNotApproved), TopicNames.BookingNotApproved,
                message.EventId.ToString(), JsonSerializer.Serialize(rejected), now, null);

            await eventRepository.AddOutboxAsync(outbox, stoppingToken);
            await processedRepository.AddAsync(message.MessageId, nameof(BookingNotApproved), stoppingToken);
            return;
        }

        await processedRepository.AddAsync(message.MessageId, nameof(BookingConfirmed), stoppingToken);
        logger.LogInformation("Места зарезервированы, BookingConfirmed в outbox. EventId={EventId}", message.EventId);
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
        consumer.Subscribe(TopicNames.BookingRequested);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null)
                    continue;

                var message = JsonSerializer.Deserialize<BookingRequested>(consumeResult.Message.Value);
                await HandleMessageAsync(message, stoppingToken);
                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке сообщения BookingRequested");
            }
        }
    }
}
