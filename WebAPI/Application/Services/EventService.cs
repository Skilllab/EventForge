using WebAPI.Application.Interfaces;
using WebAPI.Models.Domain;

namespace WebAPI.Application.Services
{
    public class EventService : IEventService
    {
        static readonly Dictionary<Guid, Event> _events = new();

        public void CreateEvent(Event newEvent)
        {
            if (_events.TryGetValue(newEvent.Id, out var value))
                throw new ArgumentException("Задание с таким ID уже существует в базе");

            _events.Add(newEvent.Id, newEvent);
        }

        public bool CancelEvent(Guid eventId)
        {
            return _events.Remove(eventId);
        }

        public List<Event> GetEvents()
        {
            return _events.Values.ToList();
        }

        public bool GetEvent(Guid eventId, out Event existedEvent)
        {
            return _events.TryGetValue(eventId, out existedEvent);
        }

        public void ChangeEvent(Event currentEvent)
        {
            if (!_events.TryGetValue(currentEvent.Id, out var existedEvent))
            {
                throw new ArgumentException("Задание с таким ID не найдено");
            }

            if (currentEvent.EndAt < currentEvent.StartAt)
            {
                throw new ArgumentException("У задания не может быть дата начала меньше даты завершения");
            }

            existedEvent.Title = currentEvent.Title;
            existedEvent.StartAt = currentEvent.StartAt;
            existedEvent.EndAt = currentEvent.EndAt;
        }
    }
}