using EventForge.Booking.Application.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventForge.Booking.Infrastructure.Services;

/// <summary>
/// Фоновый сервис для регистрации бронирований
/// </summary>
public class BookingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingBackgroundService> logger, TimeProvider timeProvider)
    : BackgroundService
{
    // Размер пакета сообщений для обработки за один раз
    private const int BatchSize = 50;

    //Задержка при обработке событий
    private const int delayForRepeatInSeconds = 5;

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        logger.LogInformation("Запущен {backgroundServiceName}", nameof(BookingBackgroundService));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<IBookingConfirmedPublisher>();

                var messages = await outboxRepository.GetPendingAsync(BatchSize, stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        await publisher.PublishRawAsync(message.Topic, message.MessageKey, message.Payload, stoppingToken);
                        await outboxRepository.MarkProcessedAsync(message.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        await outboxRepository.MarkFailedAsync(message.Id, ex.Message, stoppingToken);
                        logger.LogError(ex, "Ошибка публикации outbox сообщения {MessageId}", message.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка фоновой публикации outbox");
            }

            await Task.Delay(TimeSpan.FromSeconds(delayForRepeatInSeconds), timeProvider, stoppingToken);
        }
    }

    /// <inheritdoc/>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Остановлен {backgroundServiceName}", nameof(BookingBackgroundService));

        return base.StopAsync(cancellationToken);
    }
}
