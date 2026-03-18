using WebAPI.Application.Exceptions;
using WebAPI.Application.Interfaces;
using WebAPI.Models.Domain;
using WebAPI.Models.DTO;

namespace WebAPI.Application.Services
{
    public class EventService : IEventService
    {
        static readonly Dictionary<Guid, Event> _events = new();

        public ResponseEventDTO CreateEvent(CreateEventDTO newEventDTO)
        {

            var newEvent = Event.Create(
                newEventDTO.Title,
                newEventDTO.StartAt,
                newEventDTO.EndAt,
                newEventDTO.Description
            );

            _events.Add(newEvent.Id, newEvent);
            return MapToDTO(newEvent);
        }

        public void CancelEvent(Guid eventId)
        {
            if (!_events.Remove(eventId))
            {
                throw new NotFoundException(nameof(Event), eventId);
            }
        }

        public PaginatedResult GetEvents(EventsFilter filter)
        {
            var query = _events.Values.AsQueryable();


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

            //var totalPages = (int)Math.Ceiling((double)filteredCount / filter.pageSize);

            return new PaginatedResult(filteredCount, items, filter.page, filter.pageSize);
        }

        public ResponseEventDTO GetEvent(Guid eventId)
        {
            if (_events.TryGetValue(eventId, out var existedEvent))
            {
                return MapToDTO(existedEvent);
            }

            throw new NotFoundException(nameof(Event), eventId);
        }

        public void ChangeEvent(Guid eventId, UpdateEventDTO currentEvent)
        {
            if (!_events.TryGetValue(eventId, out var existedEvent))
            {
                throw new NotFoundException(nameof(UpdateEventDTO), eventId, "Событие с таким ID не найдено");
            }

            if (currentEvent.EndAt < currentEvent.StartAt)
            {
                throw new ValidationCustomException(nameof(UpdateEventDTO), eventId, "У события не может быть дата начала меньше даты завершения");
            }

            existedEvent.UpdateEvent(currentEvent.Title, currentEvent.StartAt, currentEvent.EndAt, currentEvent.Description);
           
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
}