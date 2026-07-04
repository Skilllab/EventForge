namespace EventForge.Settings.JWT;

/// <summary>
/// Класс для настроек генерации токена
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Имя схемы аутентификации
    /// </summary>
    public required string SchemeName { get; set; }
    /// <summary>
    /// Секрет
    /// </summary>
    public required string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Издатель
    /// </summary>
    public required string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Получатель
    /// </summary>
    public required string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Время жизни токена в часах
    /// </summary>
    public required int Lifetime { get; set; }
}
