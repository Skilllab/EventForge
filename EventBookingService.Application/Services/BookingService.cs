using EventBookingService.Application.Common;
using EventBookingService.Application.DTO;
using EventBookingService.Application.Interfaces;
using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventBookingService.Application.Services;

/// <summary>
/// Сервис для работы с бронированием
/// </summary>
public class BookingService(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository,
    IUserRepository userRepository,
    IOptions<BookingOptions> bookingOptions,
    ITransactionService transactionService,
    ILogger<BookingService> logger,
    TimeProvider timeProvider) : IBookingService
{

    private readonly BookingOptions _bookingOptions = bookingOptions.Value;

    /// <inheritdoc/>
    public async Task<BookingInfoDTO> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct)
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

            if (existedEvent.StartAt < timeProvider.GetUtcNow())
            {
                logger.LogError("Событие уже началось и недоступно для бронирования. ID: {Id}", eventId);
                throw new BookingPastEventException(nameof(Event), eventId.ToString());
            }

            var userBooking = await bookingRepository.GetUserBooking(userId, ct);
            if (userBooking.Count > _bookingOptions.MaxBookingCount)
                throw new BookingLimitExceededException(nameof(Booking), userId.ToString(), "Превышено количество допустимых бронирований");

            // Проверяем и резервируем место (все в бизнес-слое)
            if (!existedEvent.TryReserveSeats())
                throw new NoAvailableSeatsException(nameof(Event), existedEvent.Id.ToString());

            // Создаём новое бронирование
            var newBooking = Booking.Create(
                eventId,
                userId,
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
        new
        (
            ID: newBooking.Id,
            EventID: newBooking.EventId,
            Status: newBooking.Status.ToString()
        );

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

    public async Task<bool> CancelBooking(Guid bookingId, Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException(nameof(User), userId.ToString());

        var userBooking = await bookingRepository.GetByIdAsync(bookingId, ct);
        if (userBooking == null)
            throw new NotFoundException(nameof(Booking), bookingId.ToString());

        if (userBooking.UserId != user.Id && user.Role != RoleType.Admin)
            throw new InsufficientPermissionsException(nameof(Booking), bookingId.ToString(), "У пользователя недостаточно прав для отмены бронирования");

        // Защита от повторной отмены
        if (userBooking.Status == BookingStatus.Cancelled)
            throw new InvalidOperationException($"Бронирование '{bookingId}' уже было отменено");

        // Защита от отмены отклонённых бронирований
        if (userBooking.Status == BookingStatus.Rejected)
            throw new InvalidOperationException($"Невозможно отменить уже отклонённое бронирование '{bookingId}'");

        return await transactionService.ExecuteAsync(async (txContext) =>
        {
            var existedEvent = await eventRepository.GetByIdWithLockInContextAsync(userBooking.EventId, txContext.DbContext, ct);
            if (existedEvent == null)
            {
                logger.LogError("Событие не найдено при запросе. ID: {Id}", userBooking.EventId);
                throw new NotFoundException(nameof(Event), userBooking.EventId.ToString());
            }

            userBooking.Cancel(timeProvider.GetUtcNow().UtcDateTime);

            existedEvent.ReleaseSeats();

            // Все операции сохранения в рамках одной транзакции
            await eventRepository.UpdateInContextAsync(existedEvent, txContext.DbContext, ct);
            await bookingRepository.UpdateInContextAsync(userBooking, txContext.DbContext, ct);

            logger.LogInformation("Бронирование успешно отменено. ID: {Id} ", bookingId);
            return true;
        }, ct);
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


        await transactionService.ExecuteAsync(async (txContext) =>
        {
            try
            {
                existedEvent = await eventRepository.GetByIdWithLockInContextAsync(booking.EventId, txContext.DbContext, ct);
                if (existedEvent == null)
                {
                    logger.LogWarning("Событие не найдено. ID: {Id}", booking.EventId);
                    booking.Reject(processingTime);
                    return;
                }

                booking.Confirm(processingTime);
                logger.LogInformation("Бронь {Id} подтверждена", booking.Id);
            }
            catch (Exception ex)
            {
                booking.Reject(timeProvider.GetUtcNow().UtcDateTime);
                if (existedEvent != null)
                {
                    existedEvent.ReleaseSeats();
                    await eventRepository.UpdateInContextAsync(existedEvent, txContext.DbContext, ct);
                    logger.LogInformation("Места для события {Id} восстановлены после ошибки", existedEvent.Id);
                }

                if (ex is not OperationCanceledException)
                    logger.LogError(ex, "Ошибка при обработке бронирования {ID}", booking.Id);

                throw;
            }
            finally
            {
                // Сохраняем бронь ВСЕГДА (Confirmed или Rejected)
                await bookingRepository.UpdateInContextAsync(booking, txContext.DbContext, ct);
            }

        }, ct);
    }

}
