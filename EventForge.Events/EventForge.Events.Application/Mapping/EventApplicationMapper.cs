using EventForge.Events.Application.DTO;
using EventForge.Events.Domain.Entities;

namespace EventForge.Events.Application.Mapping;

/// <summary>
/// Маппер для преобразования между Доменом и Application DTO.
/// </summary>
public static class EventApplicationMapper
{
    /// <summary>
    /// Входной DTO -> Доменная модель (Создание новой сущности)
    /// </summary>
    public static Event ToDomain(this CreateEventDto dto) =>
        Event.Create(
            title: dto.Title,
            startDate: dto.StartAt,
            endDate: dto.EndAt,
            totalSeats: dto.TotalSeats,
            description: dto.Description
        );

    /// <summary>
    /// Доменная модель -> Выходной DTO
    /// </summary>
    public static EventDTO ToDto(this Event domain) =>
        new(
            Id: domain.Id,
            Title: domain.Title,
            Description: domain.Description,
            StartAt: domain.StartAt,
            EndAt: domain.EndAt,
            TotalSeats: domain.TotalSeats,
            AvailableSeats: domain.AvailableSeats
        );
}