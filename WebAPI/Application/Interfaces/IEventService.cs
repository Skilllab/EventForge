using WebAPI.Models.DTO;

namespace WebAPI.Application.Interfaces;

public interface IEventService
{
    ResponseEventDTO CreateEvent(CreateEventDTO currentEvent);
    void CancelEvent(Guid eventId);
    List<ResponseEventDTO> GetEvents();
    ResponseEventDTO GetEvent(Guid eventId);
    void ChangeEvent(Guid eventId, UpdateEventDTO currentEvent);
}