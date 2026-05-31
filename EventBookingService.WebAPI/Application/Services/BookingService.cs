using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;
using EventBookingService.Domain.Interfaces;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.DTO.Booking;

namespace EventBookingService.WebAPI.Application.Services;

/// <summary>
/// Сервис для работы с бронированием
/// </summary>
public class BookingService(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository,
    ITransactionService transactionService,
    ILogger<BookingService> logger,
    TimeProvider timeProvider) : IBookingService
{
    private static readonly SemaphoreSlim _semaphoreUpdate = new(1, 1);
    /// <inheritdoc/>
    public async Task<BookingInfoDTO> CreateBookingAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Создание нового бронирования для события: {Event}", eventId);

        return await transactionService.ExecuteAsync(async (txContext) =>
        {
            // Получаем событие с блокировкой FOR UPDATE внутри транзакции
            var existedEvent = await eventRepository.GetByIdWithLockInContextAsync(eventId, txContext.DbContext, ct);
            if (existedEvent == null)
            {
                logger.LogError("Событие не найдено при запросе. ID: {Id}", eventId);
                throw new NotFoundException(nameof(Event), eventId.ToString());
            }

            // Проверяем и резервируем место (все в бизнес-слое)
            if (!existedEvent.TryReserveSeats())
                throw new NoAvailableSeatsException(nameof(Event), existedEvent.Id.ToString());

            // Создаём новое бронирование
            var newBooking = Booking.Create(
                eventId,
                timeProvider.GetUtcNow().UtcDateTime
            );

            // Все операции сохранения в рамках одной транзакции
            await eventRepository.UpdateInContextAsync(existedEvent, txContext.DbContext, ct);
            await bookingRepository.AddInContextAsync(newBooking, txContext.DbContext, ct);

            logger.LogInformation("Бронирование успешно создано. ID: {Id} ", newBooking.Id);
            return MapToDTO(newBooking);
        }, ct);
    }


    private static BookingInfoDTO MapToDTO(Booking newBooking) =>
        new()
        {
            ID = newBooking.Id,
            EventID = newBooking.EventId,
            Status = newBooking.Status.ToString(),
        };

    /// <inheritdoc/>
    public async Task<BookingInfoDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Получение бронирования : {bookingId}", bookingId);

        var booking = await bookingRepository.GetByIdAsync(bookingId, ct);
        return booking == null
            ? throw new NotFoundException(nameof(Booking), bookingId.ToString())
            : MapToDTO(booking);
    }


    /// <inheritdoc />
    public async Task UpdateBookingAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var pendingBookings = await bookingRepository.GetAllAsync(BookingStatus.Pending, ct);
        var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, ct));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken ct)
    {
        Event? existedEvent = null;
        var processingTime = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            existedEvent = await eventRepository.GetByIdAsync(booking.EventId, ct);

            if (existedEvent == null)
            {
                logger.LogWarning("Событие не найдено. ID: {Id}", booking.EventId);
                booking.Reject(processingTime);
                return;
            }

            await _semaphoreUpdate.WaitAsync(ct);
            try
            {
                booking.Confirm(processingTime);
                logger.LogInformation("Бронь {Id} подтверждена", booking.Id);
            }
            finally
            {
                _semaphoreUpdate.Release();
            }
        }
        catch (Exception ex)
        {
            booking.Reject(timeProvider.GetUtcNow().UtcDateTime);
            if (existedEvent != null)
            {
                existedEvent.ReleaseSeats();
                await eventRepository.UpdateAsync(existedEvent, ct);
                logger.LogInformation("Места для события {Id} восстановлены после ошибки", existedEvent.Id);
            }

            if (ex is not OperationCanceledException)
                logger.LogError(ex, "Ошибка при обработке бронирования {ID}", booking.Id);

            throw;
        }
        finally
        {
            // Сохраняем бронь ВСЕГДА (Confirmed или Rejected)
            await bookingRepository.UpdateAsync(booking, ct);
        }

    }
}
