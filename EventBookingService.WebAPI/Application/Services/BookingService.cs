using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;
using EventBookingService.Domain.Interfaces;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.DTO.Booking;

namespace EventBookingService.WebAPI.Application.Services;

/// <summary>
/// Сервис для работы с бронированием
/// </summary>
public class BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository, ILogger<BookingService> logger, TimeProvider timeProvider) : IBookingService
{
    private readonly SemaphoreSlim _semaphoreCreate = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _semaphoreUpdate = new SemaphoreSlim(1, 1);

    private const int delayConnectToBaseInSeconds = 2;

    /// <inheritdoc/>
    public async Task<BookingInfoDTO> CreateBookingAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Создание нового бронирования для события: {Event}", eventId);

        var existedEvent = await eventRepository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            logger.LogError("Событие не найдено при запросе. ID: {Id}", eventId);
            throw new NotFoundException(nameof(Event), eventId);
        }

        await _semaphoreCreate.WaitAsync(ct);

        try
        {
            if (!existedEvent.TryReserveSeats())
                throw new NoAvailableSeatsException(nameof(Event), existedEvent.Id.ToString());

            var newBooking = Booking.Create(
                eventId,
                timeProvider.GetUtcNow().UtcDateTime
            );

            await bookingRepository.AddAsync(newBooking, ct);
            await eventRepository.UpdateAsync(existedEvent, ct);
            logger.LogInformation("Бронирование успешно создано. ID: {Id} ", newBooking.Id);
            return MapToDTO(newBooking);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Процесс создания брони для события с ID: {Id} прерван", existedEvent.Id);
            throw;
        }
        finally
        {
            _semaphoreCreate.Release();
        }
    }

    private BookingInfoDTO MapToDTO(Booking newBooking)
    {
        return new BookingInfoDTO()
        {
            ID = newBooking.Id,
            EventID = newBooking.EventId,
            Status = newBooking.Status.ToString(),
        };
    }

    /// <inheritdoc/>
    public async Task<BookingInfoDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Получение бронирования : {bookingId}", bookingId);

        var booking = await bookingRepository.GetByIdAsync(bookingId, ct);
        if (booking == null)
            throw new NotFoundException(nameof(Booking), bookingId);

        return MapToDTO(booking);
    }


    /// <inheritdoc />
    public async Task UpdateBookingAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var pendingBookings = await bookingRepository.GetAll(BookingStatus.Pending, ct);
        var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, ct));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken ct)
    {
        Event? existedEvent = null;
        var processingTime = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delayConnectToBaseInSeconds), timeProvider, ct);

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
            // 5. Сохраняем бронь ВСЕГДА (Confirmed или Rejected)
            await bookingRepository.UpdateAsync(booking, ct);
        }

    }
}
