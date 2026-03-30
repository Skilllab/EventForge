using System.Collections.Concurrent;

using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.Domain;

namespace EventBookingService.WebAPI.Infrastructure.Persistence
{
    public class InMemoryBookingRepository : IBookingRepository
    {
        private static readonly ConcurrentDictionary<Guid, Booking> _bookings = new();

        /// <inheritdoc/>
        public Task AddAsync(Booking booking, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            _bookings.TryAdd(booking.Id, booking);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            return Task.FromResult(_bookings.TryRemove(id, out _));
        }

        /// <inheritdoc/>
        public Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            _bookings.TryGetValue(id, out var booking);
            return Task.FromResult(booking);
        }

        /// <inheritdoc/>
        public IQueryable<Booking> GetAll(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            return _bookings.Values.AsQueryable();
        }

        /// <inheritdoc/>
        public Task UpdateAsync(Booking booking, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            _bookings[booking.Id] = booking;
            return Task.CompletedTask;
        }
    }
}
