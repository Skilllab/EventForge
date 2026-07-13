using System.Text.Json;

using EventForge.Events.Application.DTO;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Application.Mapping;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace EventForge.Events.Application.Services;

/// <summary>
/// Сервис обработки событий
/// </summary>
public class EventService(IEventRepository repository, ILogger<EventService> logger, ICacheService cache, TimeProvider timeProvider) : IEventService
{
    public async Task<EventDTO> CreateEventAsync(CreateEventDto newEventDTO, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Создание нового события: {Title}", newEventDTO.Title);
        var newEvent = Event.Create(
            newEventDTO.Title,
            newEventDTO.StartAt,
            newEventDTO.EndAt,
            newEventDTO.TotalSeats,
            newEventDTO.Description
        );

        await repository.AddAsync(newEvent, ct);
        logger.LogInformation("Событие успешно создано. ID: {Id}", newEvent.Id);
        return newEvent.ToDto();
    }

    public async Task CancelEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogDebug("Попытка удаления события с ID: {Id}", eventId);
        if (!await repository.DeleteAsync(eventId, ct))
        {
            throw new NotFoundException(nameof(Event), eventId.ToString());
        }

        logger.LogInformation("Событие успешно удалено. ID: {Id} ", eventId);
    }

    public async Task<PaginatedResultDTO> GetEventsAsync(EventsFilterDTO filter, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        logger.LogInformation("Запрос списка событий в {Now}. Фильтр: {Filter}", now, filter.Title);

        var result = await repository.GetPagedAsync(filter.Title, filter.From, filter.To, filter.Page, filter.PageSize, ct);
        var items = result.Items.Select(r => r.ToDto()).ToList();

        return new PaginatedResultDTO(result.TotalCount, items, filter.Page, filter.PageSize);
    }


    public async Task<EventDTO> GetEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var cacheKey = $"event:{eventId}";

        var cachedData = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cachedEvent = JsonSerializer.Deserialize<EventDTO>(cachedData);
            if (cachedEvent != null) return cachedEvent;
        }

        var existedEvent = await repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            logger.LogError("Событие не найдено при запросе. ID: {Id}", eventId);
            throw new NotFoundException(nameof(Event), eventId.ToString());
        }
        var currentEvent = existedEvent.ToDto();

        var serialized = JsonSerializer.Serialize(currentEvent);
        await cache.SetStringAsync(cacheKey, serialized, TimeSpan.FromMinutes(10)); 

        return currentEvent;
    }

    public async Task ChangeEventAsync(Guid eventId, UpdateEventDto currentEvent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Обновление события {Id}", eventId);
        var existedEvent = await repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            logger.LogError("Ошибка обновления: событие не существует. ID: {Id}", eventId);
            throw new NotFoundException(nameof(Event), eventId.ToString(), "Событие с таким ID не найдено");
        }

        existedEvent.UpdateEvent(
            currentEvent.Title ?? existedEvent.Title,
            currentEvent.StartAt ?? existedEvent.StartAt,
            currentEvent.EndAt ?? existedEvent.EndAt,
            currentEvent.Description ?? existedEvent.Description);

        await repository.UpdateAsync(existedEvent, ct);
        await cache.RemoveAsync($"event:{eventId}");

        logger.LogInformation("Событие успешно обновлено. ID: {Id}", eventId);
    }


    public async Task ReleaseSeatAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existedEvent = await repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
            throw new NotFoundException(nameof(Event), eventId.ToString());

        existedEvent.ReleaseSeats();
        await repository.UpdateAsync(existedEvent, ct);
        await cache.RemoveAsync($"event:{eventId}");
    }

    public async Task<PaginatedResultTop10DTO> GetTop10EventsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        logger.LogInformation("Запрос списка топ 10 событий в {Now}.", now);

        var result = await repository.GetTop10EventsAsync(ct);
        var items = result.Items.Select(r => r.ToDto()).ToList();

        return new PaginatedResultTop10DTO(items);
    }
}
