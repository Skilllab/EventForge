namespace EventForge.Booking.Infrastructure.Common;

/// <summary>
/// Настройки подключения к микросервису событий.
/// </summary>
public class EventsServiceOptions
{
    /// <summary>
    /// Базовый адрес Events API.
    /// Пример: http://localhost:5009/
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Таймаут HTTP-запросов в секундах.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
