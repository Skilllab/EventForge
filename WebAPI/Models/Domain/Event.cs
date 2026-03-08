namespace WebAPI.Models.Domain;

/// <summary>
/// Событие сервиса управления
/// </summary>
public class Event
{
    /// <summary>
    /// Уникальный идентификатор события
    /// </summary>
    public Guid Id { get;  }

    /// <summary>
    /// Название события
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; }
    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime StartAt { get; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    public DateTime EndAt { get; }


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
            throw new ArgumentException("Дата окончания события не может быть раньше даты начала");

        return new Event(title, startDate, endDate, description);
    }
}