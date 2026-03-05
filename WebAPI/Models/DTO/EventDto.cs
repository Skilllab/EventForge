using System.ComponentModel.DataAnnotations;

namespace WebAPI.Models.DTO;

/// <summary>
/// DTO класс для получения данных
/// </summary>
public class EventDto
{
    /// <summary>
    /// Уникальный идентификатор события
    /// </summary>
    [Required(ErrorMessage = "Идентификатор обязателен для заполнения")]
    public Guid Id { get; set; }

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
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    [Required(ErrorMessage = "Дата окончания события не может быть пустой")]
    public DateTime EndAt { get; set; }
}