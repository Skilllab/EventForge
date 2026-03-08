namespace WebAPI.Models.DTO;

/// <summary>
/// DTO класс для изменения события
/// </summary>
public class UpdateEventDTO
{
    public string Title { get; init; }
    public string Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
}