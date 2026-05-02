namespace EventBookingService.Domain.Entities
{
    public record PagedResult<T>(List<T> Items, long TotalCount);
}
