using System.Text.Json;

using EventForge.Booking.Application.Common;
using EventForge.Booking.Application.DTO;
using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Domain.Exceptions;
using EventForge.Contract.Brokers;
using EventForge.Shared.Entities.Enums;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Booking.Application.Services;

/// <summary>
/// Сервис для работы с бронированием.
/// </summary>
public class BookingService(
    IBookingRepository bookingRepository,
    IOptions<BookingOptions> bookingOptions,
    ILogger<BookingService> logger,
    TimeProvider timeProvider) : IBookingService
{
    private readonly BookingOptions _bookingOptions = bookingOptions.Value;

    public async Task<BookingInfoDTO> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Создание нового бронирования для события: {Event}", eventId);

        var activeBookingsCount = await bookingRepository.GetUserActiveBookingsCountAsync(userId, ct);
        if (activeBookingsCount >= _bookingOptions.MaxBookingCount)
        {
            throw new BookingLimitExceededException(
                nameof(BookingModel),
                userId.ToString(),
                $"Превышено количество допустимых бронирований. Допустимо: {_bookingOptions.MaxBookingCount}");
        }

        var newBooking = BookingModel.Create(
            eventId,
            userId,
            timeProvider.GetUtcNow().UtcDateTime);

        await bookingRepository.AddAsync(newBooking, ct);

        logger.LogInformation("Бронирование успешно создано. ID: {Id}", newBooking.Id);
        return MapToDTO(newBooking);
    }

    public async Task<BookingInfoDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Получение бронирования: {bookingId}", bookingId);

        var booking = await bookingRepository.GetByIdAsync(bookingId, ct);
        return booking == null
            ? throw new NotFoundException(nameof(BookingModel), bookingId.ToString())
            : MapToDTO(booking);
    }

    public async Task<bool> CancelBooking(Guid bookingId, Guid userId, RoleType userRole, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var userBooking = await bookingRepository.GetByIdAsync(bookingId, ct);
        if (userBooking == null)
        {
            throw new NotFoundException(nameof(BookingModel), bookingId.ToString());
        }

        if (userBooking.UserId != userId && userRole != RoleType.Admin)
        {
            throw new InsufficientPermissionsException(
                nameof(BookingModel),
                bookingId.ToString(),
                "У пользователя недостаточно прав для отмены бронирования");
        }

        if (userBooking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException($"Бронирование '{bookingId}' уже было отменено");
        }

        if (userBooking.Status == BookingStatus.Rejected)
        {
            throw new InvalidOperationException($"Невозможно отменить уже отклонённое бронирование '{bookingId}'");
        }

        userBooking.Cancel(timeProvider.GetUtcNow().UtcDateTime);

        await bookingRepository.UpdateAsync(userBooking, ct);

        logger.LogInformation("Бронирование успешно отменено. ID: {Id}", bookingId);
        return true;
    }

    public async Task UpdateBookingAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var pendingBookings = await bookingRepository.GetAllAsync(BookingStatus.Pending, ct);
        var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, ct));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessBookingAsync(BookingModel booking, CancellationToken ct)
    {
        var processingTime = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            var message = new BookingConfirmed(
                Guid.NewGuid(),
                booking.Id,
                booking.EventId,
                booking.UserId,
                1,
                processingTime);

            var outbox = OutboxMessage.Create(
                type: nameof(BookingConfirmed),
                topic: TopicNames.BookingConfirmed,
                messageKey: booking.EventId.ToString(),
                payload: JsonSerializer.Serialize(message),
                createdAt: processingTime,
                error: null);

            var saved = await bookingRepository.ConfirmAndAddOutboxAsync(
                booking.Id,
                processingTime,
                outbox,
                ct);

            if (!saved)
            {
                logger.LogWarning(
                    "Не удалось подтвердить бронирование {BookingId}. Возможно, оно уже было обработано.",
                    booking.Id);
                return;
            }

            logger.LogInformation("Бронь {Id} подтверждена и записана в Outbox", booking.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Ошибка при обработке бронирования {Id}", booking.Id);
            throw;
        }
    }

    private static BookingInfoDTO MapToDTO(BookingModel booking) =>
        new(
            ID: booking.Id,
            EventID: booking.EventId,
            Status: booking.Status.ToString());
}
