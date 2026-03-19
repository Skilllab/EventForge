using System.Net;

namespace EventBookingService.WebAPI.Models.Domain;

/// <summary>
/// Класс ApiResult c возвращаемыми данными
/// </summary>
/// <typeparam name="T">Тип данных результата</typeparam>
public class ApiResult<T> : ApiBaseResult
{
    /// <summary>
    /// Возвращаемые данные метода
    /// </summary>
    public required T Data { get; set; }
}

// Класс ApiResult без возвращаемых данных
// Наследуемся от базового класса с основными параметрами
public class ApiResult : ApiBaseResult { }

// Базовый класс с основными параметрами
public class ApiBaseResult
{
    /// <summary>
    /// Флаг, указывающий на успешность выполненного запроса
    /// </summary>
    public required bool Success { get; set; }
    /// <summary>
    /// Возвращаемый HTTP-код
    /// </summary>
    public required HttpStatusCode StatusCode { get; set; }
    /// <summary>
    /// Дата и время ответа
    /// </summary>
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Сообщение с дополнительной информацией
    /// </summary>
    public required string Message { get; set; }
}