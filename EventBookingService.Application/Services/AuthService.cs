using EventBookingService.Application.Interfaces;
using EventBookingService.Domain.Entities;

namespace EventBookingService.Application.Services
{
    public class AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator) : IAuthService
    {
       
        public async Task<bool> RegisterUserAsync(string login, string password, RoleType role)
        {
            // Проверяем уникальность через репозиторий
            if (await userRepository.ExistsAsync(login))
                return false;

            var hash = passwordHasher.HashPassword(password);
            var user = User.Create(login, hash, role);

            await userRepository.AddAsync(user);
            return true;

        }

        public async Task<string> LoginUserAsync(string login, string password)
        {
            // Ищем пользователя по логину
            var user = await userRepository.GetByLoginAsync(login);
            if (user == null)
            {
                return null; // Пользователь не найден
            }

            // Проверяем, совпадает ли введенный пароль с хэшем из базы
            var isPasswordValid = passwordHasher.VerifyPassword(password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return null; // Пароль неверный
            }

            // Генерируем и возвращаем JWT-токен
            return tokenGenerator.GenerateToken(user.Login, user.Role.ToString());
        }
    }
}
