using EventBookingService.WebAPI.Models.Domain;

namespace EventBookingService.WebAPI.Application.Interfaces
{
    public interface IEventRepository
    {
        // Работа с событиями
        Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<PagedResult<Event>> GetPagedAsync(string? title,
            DateTime? startAt,
            DateTime? endAt,
            int page,
            int pageSize,
            CancellationToken ct);
        Task AddAsync(Event @event, CancellationToken ct);
        Task UpdateAsync(Event @event, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}
