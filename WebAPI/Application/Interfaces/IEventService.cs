using EventBookingService.WebAPI.Models.DTO;

namespace EventBookingService.WebAPI.Application.Interfaces;

public interface IEventService
{
    ResponseEventDTO CreateEvent(CreateEventDTO currentEvent);

    void CancelEvent(Guid eventId);

    PaginatedResult GetEvents(EventsFilter filter );

    ResponseEventDTO GetEvent(Guid eventId);

    void ChangeEvent(Guid eventId, UpdateEventDTO currentEvent);
}