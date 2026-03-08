using WebAPI.Models.Domain;
using WebAPI.Models.DTO;

namespace WebAPI.Application.Interfaces
{
    public interface IEventService
    {
        ResponseEventDTO CreateEvent(CreateEventDTO currentEvent);
        bool CancelEvent(Guid eventId);
        List<ResponseEventDTO> GetEvents();
        bool GetEvent(Guid eventId, out ResponseEventDTO @event);
        void ChangeEvent(Event currentEvent);
    }
}