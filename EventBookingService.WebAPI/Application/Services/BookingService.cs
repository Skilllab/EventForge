using EventBookingService.WebAPI.Application.Exceptions;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.Domain;
using EventBookingService.WebAPI.Models.DTO.Booking;

namespace EventBookingService.WebAPI.Application.Services
{
    /// <summary>
    /// Сервис для работы с бронированием
    /// </summary>
    public class BookingService(IEventService eventService, IBookingRepository repository, IEventRepository repository2,  ILogger<BookingService> logger) : IBookingService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <inheritdoc/>
        public async Task<BookingInfoDTO> CreateBookingAsync(Guid eventId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            logger.LogInformation("Создание нового бронирования для события: {Event}", eventId);

            var existedEvent = await repository2.GetByIdAsync(eventId, ct);
            if (existedEvent == null)
            {
                logger.LogError("Событие не найдено при запросе. ID: {Id}", eventId);
                throw new NotFoundException(nameof(Event), eventId);
            }

            await _semaphore.WaitAsync(ct);

            try
            {
                if (!existedEvent.TryReserveSeats())
                    throw new  NoAvailableSeatsException(nameof(Event), existedEvent.Id.ToString());

                var newBooking = Booking.Create(
                    eventId,
                    DateTime.Now
                );

                await repository.AddAsync(newBooking, ct);
                await repository2.UpdateAsync(existedEvent, ct);
                logger.LogInformation("Бронирование успешно создано. ID: {Id} ", newBooking.Id);
                return MapToDTO(newBooking);
            }
            finally
            {
                _semaphore.Release();
            }
           
        }

        private BookingInfoDTO MapToDTO(Booking newBooking)
        {
            return new BookingInfoDTO()
            {
                ID = newBooking.Id, EventID = newBooking.EventId, Status = newBooking.Status.ToString(),
            };
        }

        /// <inheritdoc/>
        public async Task<BookingInfoDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
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
