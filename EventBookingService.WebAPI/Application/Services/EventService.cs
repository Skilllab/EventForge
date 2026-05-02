using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;
using EventBookingService.Domain.Interfaces;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.DTO.Events;

namespace EventBookingService.WebAPI.Application.Services;

public class EventService(IEventRepository _repository, ILogger<EventService>_logger, TimeProvider timeProvider) : IEventService
{
    /// <inheritdoc/>
    public async Task<ResponseEventDTO> CreateEventAsync(CreateEventDTO newEventDTO, CancellationToken ct)
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
        return MapToDTO(newEvent);
    }

    /// <inheritdoc/>
    public async Task CancelEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug("Попытка удаления события с ID: {Id}", eventId);
        if (!await _repository.DeleteAsync(eventId, ct))
        {
            throw new NotFoundException(nameof(Event), eventId);
        }
        _logger.LogInformation("Событие успешно удалено. ID: {Id} ", eventId);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult> GetEventsAsync(EventsFilter filter, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Получаем текущее время через провайдер (может пригодиться для логов или доп. логики)
        var now = timeProvider.GetUtcNow().UtcDateTime;

        _logger.LogInformation("Запрос списка событий в {Now}. Фильтр: {Filter}", now, filter.title);

        var result = await _repository.GetPagedAsync(filter.title, filter.from, filter.to, filter.page, filter.pageSize, ct);

        var items = result.Items.Select(MapToDTO).ToList();

        return new PaginatedResult(result.TotalCount, items, filter.page, filter.pageSize);
    }

    /// <inheritdoc/>
    public async Task<ResponseEventDTO> GetEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existedEvent = await _repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            _logger.LogError("Событие не найдено при запросе. ID: {Id}", eventId);
            throw new NotFoundException(nameof(Event), eventId);
        }

        return MapToDTO(existedEvent);
    }

    /// <inheritdoc/>
    public async Task ChangeEventAsync(Guid eventId, UpdateEventDTO currentEvent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation("Обновление события {Id}", eventId);
        var existedEvent = await _repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            _logger.LogError("Ошибка обновления: событие не существует. ID: {Id}", eventId);
            throw new NotFoundException(nameof(Event), eventId, "Событие с таким ID не найдено");
        }

        // Проверка для Nullable типов внутри сервиса
        if (currentEvent.StartAt.HasValue && currentEvent.EndAt.HasValue &&
            currentEvent.EndAt.Value < currentEvent.StartAt.Value)
        {
            _logger.LogWarning("Ошибка валидации дат для события {Id}", eventId);
            throw new ValidationCustomException(nameof(UpdateEventDTO), eventId, "У события не может быть дата начала меньше даты завершения");
        }


        existedEvent.UpdateEvent(
            currentEvent.Title ?? existedEvent.Title,
            currentEvent.StartAt ?? existedEvent.StartAt,
            currentEvent.EndAt ?? existedEvent.EndAt,
            currentEvent.Description ?? existedEvent.Description);

        await _repository.UpdateAsync(existedEvent, ct);
        _logger.LogInformation("Событие успешно обновлено. ID: {Id}", eventId);
    }

    /// <summary>
    /// Метод для мапирования из события в DTO 
    /// </summary>
    /// <param name="currentEvent">Доменное событие</param>
    /// <returns>DTO для отправки</returns>
    private ResponseEventDTO MapToDTO(Event currentEvent)
    {
        return new ResponseEventDTO
        {
            Id = currentEvent.Id,
            Title = currentEvent.Title,
            Description = currentEvent.Description,
            StartAt = currentEvent.StartAt,
            EndAt = currentEvent.EndAt,
            TotalSeats = currentEvent.TotalSeats,
            AvailableSeats = currentEvent.AvailableSeats
        };
    }
}
