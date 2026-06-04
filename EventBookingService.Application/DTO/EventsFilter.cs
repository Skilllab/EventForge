using System.ComponentModel.DataAnnotations;

using EventBookingService.Application.ValidationAttributes;

namespace EventBookingService.Application.DTO;

/// <summary>
/// Фильтр получения событий
/// </summary>
public class EventsFilter
{
    /// <summary>
    /// Название события
    /// </summary>
    [StringLength(100, ErrorMessage = "Название для поиска слишком длинное")]
    public string? title { get; set; }

    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime? from { get; set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    [DateGreater(nameof(from))]
    public DateTime? to { get; set; }

    /// <summary>
    /// Страница, которую необходимо вернуть
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть больше 0")]
    public int page { get; set; } = 1;

    /// <summary>
    /// Количество элементов на странице
    /// </summary>
    [Range(1, 100, ErrorMessage = "Размер страницы должен быть от 1 до 100 элементов")]
    public int pageSize { get; set; } = 10;
}
