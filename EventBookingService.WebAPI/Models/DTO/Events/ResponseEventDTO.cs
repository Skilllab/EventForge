namespace EventBookingService.WebAPI.Models.DTO.Events;

/// <summary>
/// DTO класс для ответов
/// </summary>
public class ResponseEventDTO
{
    /// <summary>
    /// Уникальный идентификатор события
    /// </summary>
    public Guid Id { get; init; }
    /// <summary>
    /// Название события
    /// </summary>
    public required string Title { get; init; }
    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; init; }
    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime StartAt { get; init; }
    /// <summary>
    /// Дата завершения события
    /// </summary>
    public DateTime EndAt { get; init; }

    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    public int TotalSeats { get; init; }

    /// <summary>
    /// Текущее количество свободных мест
    /// </summary>
    public int AvailableSeats { get; init; }
}
