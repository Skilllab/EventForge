using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.Domain;

namespace EventBookingService.WebAPI.Application.Services
{

    /// <summary>
    /// Фоновый сервис для регистрации бронирований
    /// </summary>
    /// <param name="scopeFactory"></param>
    /// <param name="logger"></param>
    public class BookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingBackgroundService> logger)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            logger.LogInformation("Запущен {backgroundServiceName}",  nameof(BookingBackgroundService));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    await bookingRepository.UpdateBookingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при обработке бронирований");
                }
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Остановлен {backgroundServiceName}", nameof(BookingBackgroundService));

            return base.StopAsync(cancellationToken);
        }
    }
}
