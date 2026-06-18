using EventBookingService.Domain.Entities;

namespace EventBookingService.Presentation.DTO
{
    public class NewUserRequest
    {
        public string Login{ get; set; }
        public string Password{ get; set; }
        public RoleType Role{ get; set; } = RoleType.User;

    }
}
