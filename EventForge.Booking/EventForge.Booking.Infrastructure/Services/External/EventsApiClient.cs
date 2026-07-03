using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using EventForge.Booking.Application.DTO;
using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Domain.Exceptions;


namespace EventForge.Booking.Infrastructure.Services.External;

/// <summary>
/// REST-клиент для взаимодействия с Events-сервисом.
/// </summary>
public class EventsApiClient(HttpClient httpClient) : IEventsGateway
{
    private const string EventsPath = "Events";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EventStateDTO?> GetEventAsync(Guid eventId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"{EventsPath}/{eventId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<EventStateResponse>(JsonOptions, ct);
        return dto == null
            ? null
            : new EventStateDTO(dto.Id, dto.StartAt, dto.AvailableSeats);
    }

    public async Task<bool> TryReserveSeatAsync(Guid eventId, CancellationToken ct)
    {
        var response = await httpClient.PostAsync($"{EventsPath}/{eventId}/reserve-seat", null, ct);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Event", eventId.ToString());
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task ReleaseSeatAsync(Guid eventId, CancellationToken ct)
    {
        var response = await httpClient.PostAsync($"{EventsPath}/{eventId}/release-seat", null, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Event", eventId.ToString());
        }

        response.EnsureSuccessStatusCode();
    }

    private sealed record EventStateResponse(
        Guid Id,
        string Title,
        string? Description,
        DateTime StartAt,
        DateTime EndAt,
        int TotalSeats,
        int AvailableSeats);
}
