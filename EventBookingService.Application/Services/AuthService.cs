using EventBookingService.Application.Interfaces;
using EventBookingService.Domain.Entities;

namespace EventBookingService.Application.Services
{
    public class AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator) : IAuthService
    {
        public async Task<bool> RegisterUserAsync(string login, string password, string? role)
        {
            var enumRole = RoleType.User;

            if (!string.IsNullOrEmpty(role))
            {
                if (Enum.TryParse<RoleType>(role, ignoreCase: true, out var parsedRole)) 
                    enumRole = parsedRole;
            }

            if (await userRepository.ExistsAsync(login))
                return false;

            var hash = passwordHasher.HashPassword(password);
            var user = User.Create(login, hash, enumRole);

            await userRepository.AddAsync(user);
            return true;
        }

        public async Task<string?> LoginUserAsync(string login, string password)
        {
            var user = await userRepository.GetByLoginAsync(login);
            if (user == null)
                return null;

            var isPasswordValid = passwordHasher.VerifyPassword(password, user.PasswordHash);
            return !isPasswordValid
                ? null
                : tokenGenerator.GenerateToken(user.Id, user.Role.ToString());
        }
    }
}
