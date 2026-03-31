using EventBookingService.WebAPI.Models.Domain;

namespace EventBookingService.WebAPI.Models.DTO
{
    public class BookingInfo
    {
        public Guid ID { get; init; }
        public Guid EventID { get; init; }
        public string Status { get; init; }
    }
}
