using EventBookingService.WebAPI.Application.Exceptions;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.Domain;
using EventBookingService.WebAPI.Models.DTO;

namespace EventBookingService.WebAPI.Application.Services
{
    /// <summary>
    /// Сервис для работы с бронированием
    /// </summary>
    public class BookingService(IEventService eventService, IBookingRepository repository, ILogger<BookingService> logger) : IBookingService
    {

        /// <inheritdoc/>
        public async Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            logger.LogInformation("Создание нового бронирования для события: {Event}", eventId);

            //Поиск события выкинет ошибку в случае если событие не будет найдено
            await eventService.GetEventAsync(eventId, ct);

            var newBooking = Booking.Create(
                eventId,
                DateTime.Now
            );

            await repository.AddAsync(newBooking, ct);
            logger.LogInformation("Бронирование успешно создано. ID: {Id} ", newBooking.Id);
            return MapToDTO(newBooking);
        }

        private BookingInfo MapToDTO(Booking newBooking)
        {
            return new BookingInfo()
            {
                ID = newBooking.Id, EventID = newBooking.EventId, Status = newBooking.Status.ToString(),
            };
        }

        /// <inheritdoc/>
        public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            logger.LogInformation("Получение бронирования : {bookingId}", bookingId);

            var booking =  await repository.GetByIdAsync(bookingId, ct);
            if(booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);
                
            return MapToDTO(booking);
        }
    }
}
