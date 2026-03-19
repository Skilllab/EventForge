using System.Net;

namespace EventBookingService.WebAPI.Models.Domain;

// Класс ApiResult c возвращаемыми данными
// Наследуемся от базового класса с основными параметрами
public class ApiResult<T> : ApiBaseResult
{
    // Возвращаемые данные метода
    public required T Data { get; set; }
}

// Класс ApiResult без возвращаемых данных
// Наследуемся от базового класса с основными параметрами
public class ApiResult : ApiBaseResult { }

// Базовый класс с основными параметрами
public class ApiBaseResult
{
    // Флаг, указывающий на успешность выполненного запроса
    public required bool Success { get; set; }
    // Возвращаемый HTTP-код
    public required HttpStatusCode StatusCode { get; set; }
    // Дата и время ответа
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    // Кастомное сообщение с дополнительной информацией
    // Здесь может быть информация об ошибке в случае неуспеха
    public required string Message { get; set; }
}