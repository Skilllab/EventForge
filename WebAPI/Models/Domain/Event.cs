using WebAPI.Application.Exceptions;

namespace WebAPI.Models.Domain;

/// <summary>
/// Событие сервиса управления
/// </summary>
public class Event
{
    /// <summary>
    /// Уникальный идентификатор события
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Название события
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; private set; }
    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime StartAt { get; private set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    public DateTime EndAt { get; private set; }

    /// <summary>
    /// Обновление события
    /// </summary>
    /// <param name="title">Заголовок события</param>
    /// <param name="startDate">Дата начала события</param>
    /// <param name="endDate">Дата окончания события</param>
    /// <param name="description">Описание события</param>
    public void UpdateEvent(string title, DateTime startDate, DateTime endDate, string? description = null)
    {
        Title = title;
        StartAt = startDate;
        EndAt = endDate;
        Description = description;
    }

    private Event(string title, DateTime startDate, DateTime endDate, string? description = null) 
    {
        Id = Guid.NewGuid();
        Title = title;
        StartAt = startDate;
        EndAt = endDate;
        Description = description;
    }

    /// <summary>
    /// Метод создания события
    /// </summary>
    /// <param name="title">Заголовок события</param>
    /// <param name="startDate">Дата начала события</param>
    /// <param name="endDate">Дата окончания события</param>
    /// <param name="description">Описание события</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Event Create(string title, DateTime startDate, DateTime endDate, string? description = null)
    {
        if (endDate<startDate)
            throw new ValidationCustomException(nameof(Event), Guid.Empty, "Дата окончания события не может быть раньше даты начала");

        return new Event(title, startDate, endDate, description);
    }
}