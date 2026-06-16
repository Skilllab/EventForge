using EventBookingService.Application.DTO;

namespace EventBookingService.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(UserDto user);
    }
}
