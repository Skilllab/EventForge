namespace WebAPI.Models.DTO;

/// <summary>
/// DTO класс для ответов
/// </summary>
public class ResponseEventDTO
{
    public Guid Id { get; init; }
    public string Title { get; init; } 
    public string Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
}