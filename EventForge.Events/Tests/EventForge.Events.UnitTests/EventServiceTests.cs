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
        var cacheMock = new Mock<ICacheService>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var service = new EventService(repositoryMock.Object, loggerMock.Object, cacheMock.Object, fakeTimeProvider);
        var startAt = fakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(1);
        var dto = new CreateEventDto("Намечается баня", startAt, startAt.AddHours(2), 25, "Раз в неделю точно");

        Event? savedEvent = null;
        repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((evt, _) => savedEvent = evt)
            .Returns(Task.CompletedTask);

        var result = await service.CreateEventAsync(dto, CancellationToken.None);

        result.Title.Should().Be("Намечается баня");
        result.Description.Should().Be("Раз в неделю точно");
        result.TotalSeats.Should().Be(25);
        result.AvailableSeats.Should().Be(25);
        savedEvent.Should().NotBeNull();
        savedEvent!.Title.Should().Be("Намечается баня");
        repositoryMock.Verify(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
    }
   

    [Fact]
    [Trait("Category", "GetEvent")]
    public async Task GetEventAsync_Should_Return_Event_When_It_Exists()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var cacheMock = new Mock<ICacheService>();

        var service = new EventService(repositoryMock.Object, loggerMock.Object, cacheMock.Object, TimeProvider.System);
        var startAt = new DateTime(2025, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Конференция по новым компьютерным технологиям", startAt, startAt.AddHours(8), 100, "Раз в год");

        repositoryMock
            .Setup(x => x.GetByIdAsync(evt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);

        var result = await service.GetEventAsync(evt.Id, CancellationToken.None);

        result.Id.Should().Be(evt.Id);
        result.Title.Should().Be("Конференция по новым компьютерным технологиям");
        result.AvailableSeats.Should().Be(100);
    }

    [Fact]
    [Trait("Category", "GetEvent")]
    public async Task GetEventAsync_Should_Throw_NotFound_When_Event_Does_Not_Exist()
    {
        // Arrange
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var cacheMock = new Mock<ICacheService>();

        var service = new EventService(repositoryMock.Object, loggerMock.Object, cacheMock.Object, TimeProvider.System);
        var eventId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?) null);

        // Act
        Func<Task> act = () => service.GetEventAsync(eventId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }


    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEventsAsync_Should_Return_Paginated_Result_From_Repository()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var cacheMock = new Mock<ICacheService>();

        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var service = new EventService(repositoryMock.Object, loggerMock.Object, cacheMock.Object, fakeTimeProvider);
        var filter = new EventsFilterDTO(title: "компьютерным", page: 2, pageSize: 1);
        var eventItem = Event.Create("Конференция по новым компьютерным технологиям", fakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(1), fakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(1).AddHours(2), 50);
        var paged = new PagedResult<Event>([eventItem], 3);

        repositoryMock
            .Setup(x => x.GetPagedAsync(filter.Title, filter.From, filter.To, filter.Page, filter.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await service.GetEventsAsync(filter, CancellationToken.None);

        result.EventsTotalCount.Should().Be(3);
        result.CurrentPageNumber.Should().Be(2);
        result.EventsCountOnCurrentPage.Should().Be(1);
        result.Events.Should().ContainSingle();
        result.Events[0].Title.Should().Be("Конференция по новым компьютерным технологиям");
    }

    [Fact]
    [Trait("Category", "ChangeEvent")]
    public async Task ChangeEventAsync_Should_Update_Existing_Event_Using_Partial_Dto()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var cacheMock = new Mock<ICacheService>();

        var service = new EventService(repositoryMock.Object, loggerMock.Object, cacheMock.Object, TimeProvider.System);
        var startAt = new DateTime(2025, 8, 10, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Конференция по новым компьютерным технологиям", startAt, startAt.AddHours(2), 30, "Раз в год");
        var update = UpdateEventDto.Create(title: "Новая конференция", description: "Ежегодное мероприятие");

        repositoryMock
            .Setup(x => x.GetByIdAsync(evt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        repositoryMock
            .Setup(x => x.UpdateAsync(evt, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await service.ChangeEventAsync(evt.Id, update, CancellationToken.None);

        evt.Title.Should().Be("Новая конференция");
        evt.Description.Should().Be("Ежегодное мероприятие");
        evt.StartAt.Should().Be(startAt);
        repositoryMock.Verify(x => x.UpdateAsync(evt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "ChangeEvent")]
    public async Task ChangeEventAsync_Should_Throw_NotFound_When_Event_Does_Not_Exist()
    {
        // Arrange
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var cacheMock = new Mock<ICacheService>();

        var service = new EventService(repositoryMock.Object, loggerMock.Object, cacheMock.Object, TimeProvider.System);
        var eventId = Guid.NewGuid();
        var update = UpdateEventDto.Create(title: "Новая конференция");

        repositoryMock
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?) null);

        // Act
        Func<Task> act = () => service.ChangeEventAsync(eventId, update, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
    

    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEventAsync_Should_Throw_NotFound_When_Event_Does_Not_Exist()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var cacheMock = new Mock<ICacheService>();

        var service = new EventService(repositoryMock.Object, loggerMock.Object, cacheMock.Object, TimeProvider.System);
        var eventId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.DeleteAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> act = () => service.CancelEventAsync(eventId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        repositoryMock.Verify(x => x.DeleteAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);

    }


    [Fact]
    [Trait("Category", "ReleaseSeat")]
    public async Task ReleaseSeatAsync_Should_Throw_NotFound_When_Event_Does_Not_Exist()
    {
        // Arrange
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, loggerMock.Object, cacheMock.Object, TimeProvider.System);
        var eventId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?) null);

        // Act
        Func<Task> act = () => service.ReleaseSeatAsync(eventId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "ReleaseSeat")]
    public async Task ReleaseSeatAsync_Should_ReleaseSeat_And_Update_When_Event_Exists()
    {
        // Arrange
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var cacheMock = new Mock<ICacheService>();
        var service = new EventService(repositoryMock.Object, loggerMock.Object, cacheMock.Object, TimeProvider.System);
        var startAt = new DateTime(2025, 8, 12, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Конференция по новым компьютерным технологиям", startAt, startAt.AddHours(2), 20);
        evt.TryReserveSeats();

        repositoryMock
            .Setup(x => x.GetByIdAsync(evt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);

        // Act
        await service.ReleaseSeatAsync(evt.Id, CancellationToken.None);

        // Assert
        evt.AvailableSeats.Should().Be(20); // доменный метод ReleaseSeats() вернул место
        repositoryMock.Verify(x => x.UpdateAsync(evt, It.IsAny<CancellationToken>()), Times.Once);
    }


}
