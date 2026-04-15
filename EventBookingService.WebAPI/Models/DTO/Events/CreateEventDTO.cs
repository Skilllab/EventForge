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
    public string Title { get; set; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала события
    /// </summary>
    [Required(ErrorMessage = "Дата начала события не может быть пустой")]
    [NotMinDateTime(ErrorMessage = "Дата начал события должна быть задана")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    [Required(ErrorMessage = "Дата окончания события не может быть пустой")]
    [NotMinDateTime(ErrorMessage = "Дата окончания события должна быть задана")]
    [DateGreater(nameof(StartAt))]
    public DateTime EndAt { get; set; }
}
