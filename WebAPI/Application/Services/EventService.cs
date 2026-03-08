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

        public bool CancelEvent(Guid eventId)
        {
            return _events.Remove(eventId);
        }

        public List<ResponseEventDTO> GetEvents()
        {
            return _events.Values.Select(MapToDTO).ToList();
        }

        public bool GetEvent(Guid eventId, out ResponseEventDTO responseEvent)
        {
            if (_events.TryGetValue(eventId, out var existedEvent))
            {
                responseEvent= MapToDTO(existedEvent);
                return true;
            }

            responseEvent = null;
            return false;
        }

        public void ChangeEvent(Guid eventId, UpdateEventDTO currentEvent)
        {
            if (!_events.TryGetValue(eventId, out var existedEvent))
            {
                throw new ArgumentException("Событие с таким ID не найдено");
            }

            if (currentEvent.EndAt < currentEvent.StartAt)
            {
                throw new ArgumentException("У события не может быть дата начала меньше даты завершения");
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