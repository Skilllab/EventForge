using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

using Confluent.Kafka;

using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Domain.Exceptions;
using EventForge.Booking.Infrastructure.Entities;
using EventForge.Contract.Brokers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Booking.Infrastructure.Services;

/// <summary>
/// Consumer, который получает BookingConfirmed из Kafka
/// и переводит бронь из Pending → Confirmed.
/// </summary>
public class BookingConfirmedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<BookingConfirmedConsumer> logger) : BackgroundService
{
    /// <summary>
    /// Обрабатывает одно сообщение BookingConfirmed:
    /// 1. Проверка на дубликат (Idempotent Consumer).
    /// 2. Поиск брони по ID.
    /// 3. Перевод из Pending в Confirmed.
    /// </summary>
    private async Task HandleMessageAsync(
        BookingConfirmed? message, CancellationToken ct)
    {
        if (message == null)
        {
            logger.LogWarning(
                "Получено пустое или невалидное сообщение BookingConfirmed");
            return;
        }

        // Создаём scope, т.к. Consumer — Singleton, а репозитории — Scoped
        await using var scope = scopeFactory.CreateAsyncScope();
        var processedRepository = scope.ServiceProvider
            .GetRequiredService<IProcessedMessageRepository>();
        var bookingRepository = scope.ServiceProvider
            .GetRequiredService<IBookingRepository>();

        // ── Idempotent Consumer: проверка на повторную обработку ──
        if (await processedRepository.ExistsAsync(message.MessageId, ct))
        {
            logger.LogInformation(
                "Сообщение {MessageId} уже обработано, пропускаем",
                message.MessageId);
            return;
        }

        // ── Поиск брони по ID ──
        var booking = await bookingRepository.GetByIdAsync(
            message.BookingId, ct);

        if (booking == null)
        {
            logger.LogWarning(
                "Бронь {BookingId} не найдена для сообщения {MessageId}",
                message.BookingId, message.MessageId);

            // Всё равно помечаем сообщение обработанным,
            // чтобы не зациклиться на «битых» данных
            await processedRepository.AddAsync(
                message.MessageId, nameof(BookingConfirmed), ct);
            return;
        }

        // ── Проверка текущего статуса (защита от повторного confirm) ──
        if (booking.Status != BookingStatus.Pending)
        {
            logger.LogInformation(
                "Бронь {BookingId} уже в статусе {Status}, пропускаем подтверждение",
                message.BookingId, booking.Status);

            // Помечаем сообщение обработанным — оно свою работу уже сделало
            await processedRepository.AddAsync(
                message.MessageId, nameof(BookingConfirmed), ct);
            return;
        }

        // ── Подтверждаем бронь ──
        var confirmed = await bookingRepository.ConfirmBookingAsync(
            message.BookingId, message.ConfirmedAt, ct);

        if (!confirmed)
        {
            logger.LogWarning(
                "Не удалось подтвердить бронь {BookingId}. " +
                "Возможно, статус изменился между проверкой и сохранением.",
                message.BookingId);

            // Помечаем обработанным — повторная попытка не поможет
            await processedRepository.AddAsync(
                message.MessageId, nameof(BookingConfirmed), ct);
            return;
        }

        // ── Фиксируем факт обработки ──
        await processedRepository.AddAsync(
            message.MessageId, nameof(BookingConfirmed), ct);

        logger.LogInformation(
            "Бронь {BookingId} успешно подтверждена. MessageId={MessageId}",
            message.BookingId, message.MessageId);
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
        consumer.Subscribe(TopicNames.BookingConfirmed);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null)
                    continue;

                var message = JsonSerializer.Deserialize<BookingConfirmed>(
                    consumeResult.Message.Value);

                await HandleMessageAsync(message, stoppingToken);
                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Ошибка при обработке сообщения BookingConfirmed");
            }
        }
    }
}
