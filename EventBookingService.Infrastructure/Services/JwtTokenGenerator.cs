using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using EventBookingService.Application.Interfaces;
using EventBookingService.Infrastructure.Common;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventBookingService.Infrastructure.Services;

public class JwtTokenGenerator(IOptions<JwtSettings> options, TimeProvider timeProvider) : IJwtTokenGenerator
{
    private readonly JwtSettings _settings = options.Value;

    public string GenerateToken(string login, string role)
    {
        var now = timeProvider.GetUtcNow();
        var lifeTime = now.AddHours(_settings.Lifetime);

        // Здесь оставляем только данные пользователя и уникальный ID токена
        var claims = new[]
        {
            // Логин
            new Claim(JwtRegisteredClaimNames.Name, login),

            // Роль (стандартный тип для работы [Authorize(Roles = ...)])
            new Claim("role", role),

            // Уникальный ID токена
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Системные клеймы (iss, aud, iat, exp) настраиваем через свойства дескриптора.
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
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
