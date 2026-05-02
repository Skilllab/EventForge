using EventBookingService.Domain.Entities;

namespace EventBookingService.Data.Entities
{
    public class BookingEntity
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
