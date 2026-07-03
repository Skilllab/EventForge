using System.Net.Http.Json;
using System.Text.Json;

using EventBookingService.Application.DTO;
using EventBookingService.Application.Interfaces;
using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;

namespace EventBookingService.Infrastructure.Services.External;

/// <summary>
/// Реализация <see cref="IEventService"/> через REST-вызовы к EventForge.Events.
/// </summary>
/// <param name="httpClient">Сконфигурированный HTTP-клиент.</param>
public class EventsApiClient(HttpClient httpClient) : IEventService
{
    private const string EventsPath = "Events";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<EventDTO> CreateEventAsync(CreateEventDto currentEvent, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync(EventsPath, currentEvent, JsonOptions, ct);

        var createdEvent = await response.Content.ReadFromJsonAsync<EventDTO>(JsonOptions, ct);

        return createdEvent
               ?? throw new InvalidOperationException("Events service вернул пустой ответ при создании события.");
    }

    /// <inheritdoc />
    public async Task CancelEventAsync(Guid eventId, CancellationToken ct)
    {
        await httpClient.DeleteAsync($"{EventsPath}/{eventId}", ct);
        //await EnsureSuccessOrThrowAsync(response, eventId.ToString(), ct);
    }

    /// <inheritdoc />
    public async Task<PaginatedResultDTO> GetEventsAsync(EventsFilterDTO filter, CancellationToken ct)
    {
        var query = BuildEventsQuery(filter);
        var response = await httpClient.GetAsync($"{EventsPath}{query}", ct);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResultDTO>(JsonOptions, ct);

        return result
               ?? new PaginatedResultDTO(0, [], filter.Page, 0);
    }

    /// <inheritdoc />
    public async Task<EventDTO> GetEventAsync(Guid eventId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"{EventsPath}/{eventId}", ct);
        var eventDto = await response.Content.ReadFromJsonAsync<EventDTO>(JsonOptions, ct);

        return eventDto
               ?? throw new NotFoundException(nameof(Event), eventId.ToString());
    }

    /// <inheritdoc />
    public async Task ChangeEventAsync(Guid eventId, UpdateEventDto currentEvent, CancellationToken ct)
    {
        await httpClient.PutAsJsonAsync($"{EventsPath}/{eventId}", currentEvent, JsonOptions, ct);
    }

    private static string BuildEventsQuery(EventsFilterDTO filter)
    {
        var queryParts = new List<string>
        {
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            queryParts.Add($"title={Uri.EscapeDataString(filter.Title)}");
        }

        if (filter.From.HasValue)
        {
            queryParts.Add($"from={Uri.EscapeDataString(filter.From.Value.ToString("O"))}");
        }

        if (filter.To.HasValue)
        {
            queryParts.Add($"to={Uri.EscapeDataString(filter.To.Value.ToString("O"))}");
        }

        return $"?{string.Join("&", queryParts)}";
    }
}