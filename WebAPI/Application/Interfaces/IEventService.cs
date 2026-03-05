using WebAPI.Models.Domain;
using WebAPI.Models.DTO;

namespace WebAPI.Application.Interfaces
{
    public interface IEventService
    {
        void CreateEvent(Event currentEvent);
        bool CancelEvent(Guid eventId);
        List<Event> GetEvents();
        bool GetEvent(Guid eventId, out Event @event);
        void ChangeEvent(Event currentEvent);
    }
}