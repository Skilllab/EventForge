namespace WebAPI.Models.Domain;

public class Event
{
    /// <summary>
    /// Уникальный идентификатор события
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Название события
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Дата начала события
    /// </summary>
    public required DateTime StartAt { get; set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    public required DateTime EndAt { get; set; }
}