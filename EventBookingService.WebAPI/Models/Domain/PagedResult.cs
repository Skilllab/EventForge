namespace EventBookingService.WebAPI.Models.Domain
{
    public record PagedResult<T>(List<T> Items, long TotalCount);
}
