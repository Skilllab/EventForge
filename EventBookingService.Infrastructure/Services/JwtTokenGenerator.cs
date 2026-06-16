using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using EventBookingService.Application.Common;
using EventBookingService.Application.DTO;
using EventBookingService.Application.Interfaces;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventBookingService.Infrastructure.Services;

public class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateToken(UserDto user)
    {
        var now = timeProvider.GetUtcNow();
        var lifeTime = now.AddHours(_options.Lifetime);

        // Здесь оставляем только данные пользователя и уникальный ID токена
        var claims = new[]
        {
            // Идентификатор пользователя
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

            // Логин
            new Claim(JwtRegisteredClaimNames.Name, user.Login),

            // Роль (стандартный тип для работы [Authorize(Roles = ...)])
            new Claim(ClaimTypes.Role, user.Role.ToString()),

            // Уникальный ID токена
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Системные клеймы (iss, aud, iat, exp) настраиваем через свойства дескриптора.
        // Библиотека сама переведет их в формат Unix timestamp и добавит в токен.
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            NotBefore = now.UtcDateTime,
            Expires = lifeTime.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }
}
