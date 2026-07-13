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
    ILogger<BookingRequestedConsumer> logger, TimeProvider timeProvider) : BackgroundService
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

        var bookingEvent = await eventRepository.GetByIdAsync(message.EventId, stoppingToken);
        if (bookingEvent == null)
        {
            logger.LogWarning(
                "Событие {EventId} не найдено для сообщения {MessageId}",
                message.EventId,
                message.MessageId);

            var rejected = new BookingRejected(
                Guid.NewGuid(),
                message.BookingId,
                message.EventId,
                message.UserId,
                timeProvider.GetUtcNow().UtcDateTime, "не найдено событие");

            var outbox = OutboxMessage.Create(
                nameof(BookingRejected),
                TopicNames.BookingRejected,
                message.EventId.ToString(),
                JsonSerializer.Serialize(rejected),
                timeProvider.GetUtcNow().UtcDateTime,
                null);

            await eventRepository.AddOutboxAsync(outbox, stoppingToken);
            await processedRepository.AddAsync(message.MessageId, nameof(BookingRejected), stoppingToken);
            return;
        }

        if (bookingEvent.StartAt <= timeProvider.GetUtcNow().UtcDateTime)
        {
            var notApproved = new BookingNotApproved(
                Guid.NewGuid(),
                message.BookingId,
                message.EventId,
                message.UserId,
                timeProvider.GetUtcNow().UtcDateTime,
                BookingNotApprovedReason.EventStarted);

            var outbox = OutboxMessage.Create(
                nameof(BookingNotApproved),
                TopicNames.BookingNotApproved,
                message.EventId.ToString(),
                JsonSerializer.Serialize(notApproved),
                timeProvider.GetUtcNow().UtcDateTime,
                null);

            await eventRepository.AddOutboxAsync(outbox, stoppingToken);
            await processedRepository.AddAsync(message.MessageId, nameof(BookingNotApproved), stoppingToken);
            return;
        }

        if (!bookingEvent.TryReserveSeats(message.SeatsCount))
        {
            var notApproved = new BookingNotApproved(
                Guid.NewGuid(),
                message.BookingId,
                message.EventId,
                message.UserId,
                timeProvider.GetUtcNow().UtcDateTime,
                BookingNotApprovedReason.NoSeats);

            var outbox = OutboxMessage.Create(
                nameof(BookingNotApproved),
                TopicNames.BookingNotApproved,
                message.EventId.ToString(),
                JsonSerializer.Serialize(notApproved),
                timeProvider.GetUtcNow().UtcDateTime,
                null);

            await eventRepository.AddOutboxAsync(outbox, stoppingToken);
            await processedRepository.AddAsync(message.MessageId, nameof(BookingNotApproved), stoppingToken);
            return;
        }

        var confirmed = new BookingConfirmed(
            Guid.NewGuid(),
            message.BookingId,
            message.EventId,
            message.UserId,
            message.SeatsCount,
            timeProvider.GetUtcNow().UtcDateTime);

        var confirmedOutbox = OutboxMessage.Create(
            nameof(BookingConfirmed),
            TopicNames.BookingConfirmed,
            message.EventId.ToString(),
            JsonSerializer.Serialize(confirmed),
            timeProvider.GetUtcNow().UtcDateTime,
            null);

        await eventRepository.SaveEventAndOutboxAsync(bookingEvent, confirmedOutbox, stoppingToken);
        await processedRepository.AddAsync(message.MessageId, nameof(BookingConfirmed), stoppingToken);

        logger.LogInformation("Места зарезервированы. EventId={EventId}", message.EventId);
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
