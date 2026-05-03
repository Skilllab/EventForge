using EventBookingService.WebAPI.Application.Interfaces;

namespace EventBookingService.WebAPI.Application.Services;

/// <summary>
/// Фоновый сервис для регистрации бронирований
/// </summary>
public class BookingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingBackgroundService> logger, TimeProvider timeProvider)
    : BackgroundService
{

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
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                await bookingService.UpdateBookingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке бронирований");
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
