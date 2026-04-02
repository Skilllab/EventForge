using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.Domain;

namespace EventBookingService.WebAPI.Application.Services
{
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
                    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

                    Func<Booking, bool> query = e => e.Status == BookingStatus.Pending;

                    var pendingBookings = bookingRepository.GetAll(query, stoppingToken);

                    foreach (var booking in pendingBookings)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        booking.Status = BookingStatus.Confirmed;
                        booking.ProcessedAt = DateTime.UtcNow;
                        await bookingRepository.UpdateAsync(booking, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при подтверждении бронирования");
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
