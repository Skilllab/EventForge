namespace EventForge.Events.Infrastructure.Entities;

/// <summary>
/// Класс для хранения настроек Redis
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Строка подключения к Redis
    /// </summary>
    public string ConnectionString{ get; set; }
}
