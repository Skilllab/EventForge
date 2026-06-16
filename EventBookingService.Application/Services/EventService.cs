using EventBookingService.Application.DTO;
using EventBookingService.Application.Interfaces;
using EventBookingService.Application.Mapping;
using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace EventBookingService.Application.Services;

/// <summary>
/// Сервис обработки событий
/// </summary>
/// <param name="_repository">Репозиторий с событиями</param>
/// <param name="_logger">Логгер</param>
/// <param name="timeProvider">Провайдер управления временем и датой</param>
public class EventService(IEventRepository _repository, ILogger<EventService> _logger, TimeProvider timeProvider) : IEventService
{
    /// <inheritdoc/>
    public async Task<EventDTO> CreateEventAsync(CreateEventDto newEventDTO, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation("Создание нового события: {Title}", newEventDTO.Title);
        var newEvent = Event.Create(
            newEventDTO.Title,
            newEventDTO.StartAt,
            newEventDTO.EndAt,
            newEventDTO.TotalSeats,
            newEventDTO.Description
        );

        await _repository.AddAsync(newEvent, ct);
        _logger.LogInformation("Событие успешно создано. ID: {Id}", newEvent.Id);
        return newEvent.ToDto();
    }

    /// <inheritdoc/>
    public async Task CancelEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug("Попытка удаления события с ID: {Id}", eventId);
        if (!await _repository.DeleteAsync(eventId, ct))
        {
            throw new NotFoundException(nameof(Event), eventId.ToString());
        }
        _logger.LogInformation("Событие успешно удалено. ID: {Id} ", eventId);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResultDTO> GetEventsAsync(EventsFilterDTO filter, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Получаем текущее время через провайдер (может пригодиться для логов или доп. логики)
        var now = timeProvider.GetUtcNow().UtcDateTime;

        _logger.LogInformation("Запрос списка событий в {Now}. Фильтр: {Filter}", now, filter.Title);

        var result = await _repository.GetPagedAsync(filter.Title, filter.From, filter.To, filter.Page, filter.PageSize, ct);

        var items = result.Items.Select(r=>r.ToDto()).ToList();

        return new PaginatedResultDTO(result.TotalCount, items, filter.Page, filter.PageSize);
    }

    /// <inheritdoc/>
    public async Task<EventDTO> GetEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existedEvent = await _repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            _logger.LogError("Событие не найдено при запросе. ID: {Id}", eventId);
            throw new NotFoundException(nameof(Event), eventId.ToString());
        }

        return existedEvent.ToDto();
    }

    /// <inheritdoc/>
    public async Task ChangeEventAsync(Guid eventId, UpdateEventDto currentEvent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation("Обновление события {Id}", eventId);
        var existedEvent = await _repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            _logger.LogError("Ошибка обновления: событие не существует. ID: {Id}", eventId);
            throw new NotFoundException(nameof(Event), eventId.ToString(), "Событие с таким ID не найдено");
        }

        existedEvent.UpdateEvent(
            currentEvent.Title ?? existedEvent.Title,
            currentEvent.StartAt ?? existedEvent.StartAt,
            currentEvent.EndAt ?? existedEvent.EndAt,
            currentEvent.Description ?? existedEvent.Description);

        await _repository.UpdateAsync(existedEvent, ct);
        _logger.LogInformation("Событие успешно обновлено. ID: {Id}", eventId);
    }
}
