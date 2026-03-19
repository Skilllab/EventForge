using EventBookingService.WebAPI.Application.Exceptions;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Models.Domain;
using EventBookingService.WebAPI.Models.DTO;

namespace EventBookingService.WebAPI.Application.Services;

public class EventService(IEventRepository _repository) : IEventService
{
    public ResponseEventDTO CreateEvent(CreateEventDTO newEventDTO)
    {

        var newEvent = Event.Create(
            newEventDTO.Title,
            newEventDTO.StartAt,
            newEventDTO.EndAt,
            newEventDTO.Description
        );

        _repository.Add(newEvent);
        return MapToDTO(newEvent);
    }

    public void CancelEvent(Guid eventId)
    {
        if (!_repository.Delete(eventId))
        {
            throw new NotFoundException(nameof(Event), eventId);
        }
    }

    public PaginatedResult GetEvents(EventsFilter filter)
    {
        var query = _repository.GetAll();

        if (!string.IsNullOrEmpty(filter.title))
            query = query.Where(p => p.Title.Contains(filter.title, StringComparison.CurrentCultureIgnoreCase));

        if (filter.from.HasValue)
            query = query.Where(p => p.StartAt <= filter.from);

        if (filter.to.HasValue)
            query = query.Where(p => p.EndAt >= filter.to);

        var filteredCount = query.Count();

        var items = query
            .OrderBy(c => c.Title)
            .Skip((filter.page - 1) * filter.pageSize)
            .Take(filter.pageSize)
            .Select(MapToDTO)
            .ToList();

        return new PaginatedResult(filteredCount, items, filter.page, filter.pageSize);
    }

    public ResponseEventDTO GetEvent(Guid eventId)
    {
        var existedEvent = _repository.GetById(eventId);
        if (existedEvent == null)
            throw new NotFoundException(nameof(Event), eventId);

        return MapToDTO(existedEvent);
    }

    public void ChangeEvent(Guid eventId, UpdateEventDTO currentEvent)
    {
        var existedEvent = _repository.GetById(eventId);
        if (existedEvent == null)
            throw new NotFoundException(nameof(Event), eventId, "Событие с таким ID не найдено");

        if (currentEvent.EndAt < currentEvent.StartAt)
            throw new ValidationCustomException(nameof(UpdateEventDTO), eventId, "У события не может быть дата начала меньше даты завершения");

        existedEvent.UpdateEvent(currentEvent.Title, currentEvent.StartAt, currentEvent.EndAt, currentEvent.Description);
        _repository.Update(existedEvent);
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