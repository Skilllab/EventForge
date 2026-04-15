using EventBookingService.WebAPI.Application.Exceptions;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.Domain;
using EventBookingService.WebAPI.Models.DTO.Booking;

namespace EventBookingService.WebAPI.Application.Services
{
    /// <summary>
    /// Сервис для работы с бронированием
    /// </summary>
    public class BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository,  ILogger<BookingService> logger) : IBookingService
    {
        private readonly SemaphoreSlim _semaphoreCreate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _semaphoreUpdate = new SemaphoreSlim(1, 1);

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
                    DateTime.Now
                );

                await bookingRepository.AddAsync(newBooking, ct);
                await eventRepository.UpdateAsync(existedEvent, ct);
                logger.LogInformation("Бронирование успешно создано. ID: {Id} ", newBooking.Id);
                return MapToDTO(newBooking);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Процесс создания брони для события с ID: {Id} прерван", existedEvent.Id );
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
                ID = newBooking.Id, EventID = newBooking.EventId, Status = newBooking.Status.ToString(),
            };
        }

        /// <inheritdoc/>
        public async Task<BookingInfoDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            logger.LogInformation("Получение бронирования : {bookingId}", bookingId);

            var booking =  await bookingRepository.GetByIdAsync(bookingId, ct);
            if(booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);
                
            return MapToDTO(booking);
        }


        /// <inheritdoc />
        public async Task UpdateBookingAsync(CancellationToken ct)
        {
            Func<Booking, bool> query = e => e.Status == BookingStatus.Pending;
            var pendingBookings = bookingRepository.GetAll(query, ct);
            var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, ct));
            await Task.WhenAll(tasks);
        }

        private async Task ProcessBookingAsync(Booking booking, CancellationToken ct)
        {
            //Имитируем запросы в БД
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            var existedEvent = await eventRepository.GetByIdAsync(booking.EventId, ct);
           

            await _semaphoreUpdate.WaitAsync(ct);
            try
            {
                if (existedEvent == null)
                {
                    booking.Status = BookingStatus.Rejected;
                    logger.LogWarning("Событие не найдено при запросе бронирования. ID: {Id}", booking.EventId);
                    throw new NotFoundException(nameof(Event), booking.EventId, "В процессе бронирования событие не было найдено в хранилище");
                }

                
                booking.Confirm();
                existedEvent.TryReserveSeats();

                logger.LogInformation(
                    "Обработка события {currentBooking} завершена {date} и переведена в статус {status}",
                    booking.Id, booking.ProcessedAt, booking.Status.ToString());
            }
            catch (OperationCanceledException)
            {
                booking.Reject();
                existedEvent.ReleaseSeats();
                logger.LogWarning("Процесс подтверждения брони с ID: {Id} прерван", booking.Id);
            }
            catch (Exception e)
            {
                booking.Reject();
                existedEvent.ReleaseSeats();
                logger.LogError(e, "Ошибка при обработке подтверждения бронирования");
                throw;
            }
            finally
            {
                await bookingRepository.UpdateAsync(booking, ct);
                if (existedEvent != null)
                {
                    await eventRepository.UpdateAsync(existedEvent, ct);
                }

                _semaphoreUpdate.Release();
            }
            
        }
    }
}
