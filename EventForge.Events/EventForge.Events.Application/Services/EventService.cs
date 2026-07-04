using EventForge.Events.Application.DTO;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Application.Mapping;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Domain.Exceptions;

using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;

namespace EventForge.Events.Application.Services;

/// <summary>
/// Сервис обработки событий
/// </summary>
/// <param name="repository">Репозиторий с событиями</param>
/// <param name="logger">Логгер</param>
/// <param name="timeProvider">Провайдер управления временем и датой</param>
public class EventService(IEventRepository repository, ILogger<EventService> logger, TimeProvider timeProvider) : IEventService
{
    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<PaginatedResultDTO> GetEventsAsync(EventsFilterDTO filter, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        logger.LogInformation("Запрос списка событий в {Now}. Фильтр: {Filter}", now, filter.Title);

        var result = await repository.GetPagedAsync(filter.Title, filter.From, filter.To, filter.Page, filter.PageSize, ct);
        var items = result.Items.Select(r => r.ToDto()).ToList();

        return new PaginatedResultDTO(result.TotalCount, items, filter.Page, filter.PageSize);
    }

    /// <inheritdoc/>
    public async Task<EventDTO> GetEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existedEvent = await repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            logger.LogError("Событие не найдено при запросе. ID: {Id}", eventId);
            throw new NotFoundException(nameof(Event), eventId.ToString());
        }

        return existedEvent.ToDto();
    }

    /// <inheritdoc/>
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
        logger.LogInformation("Событие успешно обновлено. ID: {Id}", eventId);
    }

    public async Task<bool> TryReserveSeatAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var reserved = await repository.TryReserveSeatAsync(eventId, 1, ct);
        if (!reserved)
        {
            var existedEvent = await repository.GetByIdAsync(eventId, ct);
            if (existedEvent == null)
            {
                throw new NotFoundException(nameof(Event), eventId.ToString());
            }
        }

        return reserved;
    }

    public async Task ReleaseSeatAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existedEvent = await repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            throw new NotFoundException(nameof(Event), eventId.ToString());
        }

        await repository.ReleaseSeatAsync(eventId, 1, ct);
    }


}
