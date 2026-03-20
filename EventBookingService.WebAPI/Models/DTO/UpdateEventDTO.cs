using EventBookingService.WebAPI.Infrastructure.Attributes;

namespace EventBookingService.WebAPI.Models.DTO;

/// <summary>
/// DTO класс для изменения события
/// </summary>
public class UpdateEventDTO
{
    /// <summary>
    /// Название события
    /// </summary>
    public string? Title { get; init; }
    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; init; }
    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime? StartAt { get; init; }
    /// <summary>
    /// Дата завершения события
    /// </summary>
    [DateGreater(nameof(StartAt))]
    public DateTime? EndAt { get; init; }
}