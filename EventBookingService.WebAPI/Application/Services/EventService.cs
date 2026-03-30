using EventBookingService.WebAPI.Application.Exceptions;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.Domain;
using EventBookingService.WebAPI.Models.DTO;

namespace EventBookingService.WebAPI.Application.Services;

public class EventService(IEventRepository _repository, ILogger<EventService>_logger) : IEventService
{
    /// <inheritdoc/>
    public async Task<ResponseEventDTO> CreateEventAsync(CreateEventDTO newEventDTO)
    {
        _logger.LogInformation("Создание нового события: {Title}", newEventDTO.Title);
        var newEvent = Event.Create(
            newEventDTO.Title,
            newEventDTO.StartAt,
            newEventDTO.EndAt,
            newEventDTO.Description
        );

        await _repository.AddAsync(newEvent);
        _logger.LogInformation("Событие успешно создано. ID: {Id}", newEvent.Id);
        return MapToDTO(newEvent);
    }

    /// <inheritdoc/>
    public async Task CancelEventAsync(Guid eventId)
    {
        _logger.LogDebug("Попытка удаления события с ID: {Id}", eventId);
        if (!await _repository.DeleteAsync(eventId))
        {
            throw new NotFoundException(nameof(Event), eventId);
        }
        _logger.LogInformation("Событие успешно удалено. ID: {Id} ", eventId);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult> GetEventsAsync(EventsFilter filter)
    {
        _logger.LogInformation("Запрос списка событий. Страница: {Page}, Фильтр: {Filter}", filter.page, filter.title);

        var query = _repository.GetAll();

        if (!string.IsNullOrEmpty(filter.title))
            query = query.Where(p => p.Title.Contains(filter.title, StringComparison.CurrentCultureIgnoreCase));

        // события, которые начинаются не раньше указанной даты
        if (filter.from.HasValue)
            query = query.Where(p => p.StartAt >= filter.from);

        //события, которые заканчиваются не позже указанной даты
        if (filter.to.HasValue)
            query = query.Where(p => p.EndAt <= filter.to);

        var filteredCount = query.Count();

        var items = query
            .OrderBy(c => c.Title)
            .Skip((filter.page - 1) * filter.pageSize)
            .Take(filter.pageSize)
            .Select(MapToDTO)
            .ToList();

        return new PaginatedResult(filteredCount, items, filter.page, filter.pageSize);
    }

    /// <inheritdoc/>
    public async Task<ResponseEventDTO> GetEventAsync(Guid eventId)
    {
        var existedEvent = await _repository.GetByIdAsync(eventId);
        if (existedEvent == null)
        {
            _logger.LogError("Событие не найдено при запросе. ID: {Id}", eventId);
            throw new NotFoundException(nameof(Event), eventId);
        }

        return MapToDTO(existedEvent);
    }

    /// <inheritdoc/>
    public async Task ChangeEventAsync(Guid eventId, UpdateEventDTO currentEvent)
    {
        _logger.LogInformation("Обновление события {Id}", eventId);
        var existedEvent = await _repository.GetByIdAsync(eventId);
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

        await _repository.UpdateAsync(existedEvent);
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
            EndAt = currentEvent.EndAt
        };
    }
}
