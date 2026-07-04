using EventForge.Events.Application.DTO;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Application.Services;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Domain.Exceptions;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Moq;

namespace EventForge.Events.UnitTests;

public class EventServiceTests
{
    [Fact]
    [Trait("Category", "CreateEvent")]
    public async Task CreateEventAsync_Should_Return_EventDto_And_Save_Event()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
        var startAt = fakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(1);
        var dto = new CreateEventDto("Tech Meetup", startAt, startAt.AddHours(2), 25, "Quarterly meetup");

        Event? savedEvent = null;
        repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((evt, _) => savedEvent = evt)
            .Returns(Task.CompletedTask);

        var result = await service.CreateEventAsync(dto, CancellationToken.None);

        result.Title.Should().Be("Tech Meetup");
        result.Description.Should().Be("Quarterly meetup");
        result.TotalSeats.Should().Be(25);
        result.AvailableSeats.Should().Be(25);
        savedEvent.Should().NotBeNull();
        savedEvent!.Title.Should().Be("Tech Meetup");
        repositoryMock.Verify(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEventAsync_Should_Throw_NotFound_When_Event_Does_Not_Exist()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var service = new EventService(repositoryMock.Object, loggerMock.Object, TimeProvider.System);
        var eventId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.DeleteAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = () => service.CancelEventAsync(eventId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Category", "GetEvent")]
    public async Task GetEventAsync_Should_Return_Event_When_It_Exists()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var service = new EventService(repositoryMock.Object, loggerMock.Object, TimeProvider.System);
        var startAt = new DateTime(2025, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Conference", startAt, startAt.AddHours(8), 100, "Annual event");

        repositoryMock
            .Setup(x => x.GetByIdAsync(evt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);

        var result = await service.GetEventAsync(evt.Id, CancellationToken.None);

        result.Id.Should().Be(evt.Id);
        result.Title.Should().Be("Conference");
        result.AvailableSeats.Should().Be(100);
    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEventsAsync_Should_Return_Paginated_Result_From_Repository()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
        var filter = new EventsFilterDTO(title: "Meetup", page: 2, pageSize: 1);
        var eventItem = Event.Create("Meetup SPB", fakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(1), fakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(1).AddHours(2), 50);
        var paged = new PagedResult<Event>([eventItem], 3);

        repositoryMock
            .Setup(x => x.GetPagedAsync(filter.Title, filter.From, filter.To, filter.Page, filter.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await service.GetEventsAsync(filter, CancellationToken.None);

        result.EventsTotalCount.Should().Be(3);
        result.CurrentPageNumber.Should().Be(2);
        result.EventsCountOnCurrentPage.Should().Be(1);
        result.Events.Should().ContainSingle();
        result.Events[0].Title.Should().Be("Meetup SPB");
    }

    [Fact]
    [Trait("Category", "ChangeEvent")]
    public async Task ChangeEventAsync_Should_Update_Existing_Event_Using_Partial_Dto()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var service = new EventService(repositoryMock.Object, loggerMock.Object, TimeProvider.System);
        var startAt = new DateTime(2025, 8, 10, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Old title", startAt, startAt.AddHours(2), 30, "Old description");
        var update = UpdateEventDto.Create(title: "New title", description: "New description");

        repositoryMock
            .Setup(x => x.GetByIdAsync(evt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        repositoryMock
            .Setup(x => x.UpdateAsync(evt, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await service.ChangeEventAsync(evt.Id, update, CancellationToken.None);

        evt.Title.Should().Be("New title");
        evt.Description.Should().Be("New description");
        evt.StartAt.Should().Be(startAt);
        repositoryMock.Verify(x => x.UpdateAsync(evt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "TryReserveSeat")]
    public async Task TryReserveSeatAsync_Should_Throw_NotFound_When_Reserve_Failed_Because_Event_Missing()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var service = new EventService(repositoryMock.Object, loggerMock.Object, TimeProvider.System);
        var eventId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.TryReserveSeatAsync(eventId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repositoryMock
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        Func<Task> act = () => service.TryReserveSeatAsync(eventId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Category", "ReleaseSeat")]
    public async Task ReleaseSeatAsync_Should_Call_Repository_When_Event_Exists()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var service = new EventService(repositoryMock.Object, loggerMock.Object, TimeProvider.System);
        var startAt = new DateTime(2025, 8, 12, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Release event", startAt, startAt.AddHours(2), 20);
        evt.TryReserveSeats();

        repositoryMock
            .Setup(x => x.GetByIdAsync(evt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        repositoryMock
            .Setup(x => x.ReleaseSeatAsync(evt.Id, 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await service.ReleaseSeatAsync(evt.Id, CancellationToken.None);

        repositoryMock.Verify(x => x.ReleaseSeatAsync(evt.Id, 1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
