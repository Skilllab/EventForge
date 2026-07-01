namespace EventForge.Users.Infrastructure.Common;

/// <summary>
/// Настройки генерации и валидации JWT-токена.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Секрет подписи токена.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Издатель токена.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Получатель токена.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Время жизни токена в часах.
    /// </summary>
    public int Lifetime { get; set; }
}
