using EventBookingService.WebAPI.Infrastructure.Attributes;

using System.ComponentModel.DataAnnotations;

namespace EventBookingService.WebAPI.Models.DTO.Events;

/// <summary>
/// DTO класс для создания события
/// </summary>
public class CreateEventDTO
{
    /// <summary>
    /// Название события
    /// </summary>
    [Required(ErrorMessage = "Наименование для события обязательно для заполнения.")]
    public required string Title { get; set; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала события
    /// </summary>
    [Required(ErrorMessage = "Дата начала события не может быть пустой")]
    [NotMinDateTime(ErrorMessage = "Дата начала события должна быть задана")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    [Required(ErrorMessage = "Дата окончания события не может быть пустой")]
    [NotMinDateTime(ErrorMessage = "Дата окончания события должна быть задана")]
    [DateGreater(nameof(StartAt))]
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Общее количество мест для события должно быть больше нуля")]
    public int TotalSeats { get; set; }
}
