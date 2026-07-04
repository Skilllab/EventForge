namespace EventForge.Booking.Application.Interfaces;

/// <summary>
/// Unit of Work для локальной БД Booking-сервиса.
/// </summary>
public interface IBookingUnitOfWork
{
    /// <summary>
    /// Выполняет несколько операций в одной транзакции BookingDbContext
    /// </summary>
    /// <param name="action"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct);
}
