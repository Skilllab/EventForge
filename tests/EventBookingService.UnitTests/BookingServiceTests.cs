using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;
using EventBookingService.Domain.Interfaces;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Application.Services;
using EventBookingService.WebAPI.Models.DTO.Booking;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Moq;

namespace EventBookingService.Tests;

public class BookingServiceTests
{
    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_ShouldSaveBooking_WhenEventExists1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();

        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        var ct = CancellationToken.None;

        var existingEvent = Event.Create("Свежее тестовое событие", now, now.AddHours(1), 1);
        eventRepositoryMock
            .Setup(r => r.GetByIdAsync(existingEvent.Id, ct))
            .ReturnsAsync(existingEvent);

        // Act
        var result = await service.CreateBookingAsync(existingEvent.Id, ct);

        // Assert
        result.Status.Should().Be(nameof(BookingStatus.Pending));
        bookingRepositoryMock.Verify(r => r.AddAsync(It.Is<Booking>(b => b.EventId == existingEvent.Id), ct), Times.Once);
        eventRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Event>(e => e.AvailableSeats == 0), ct), Times.Once);
        eventRepositoryMock.Verify(r => r.GetByIdAsync(existingEvent.Id, ct), Times.Once);
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_RunManyTimes_ShouldSaveBooking_WhenEventExists1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();

        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, loggerMock.Object, fakeTimeProvider);

        var ct = CancellationToken.None;
        var totalSeats = 10;
        var existingEvent = Event.Create("Свежее тестовое событие 2", now, now.AddHours(1), totalSeats);

        eventRepositoryMock
            .Setup(r => r.GetByIdAsync(existingEvent.Id, ct))
            .ReturnsAsync(existingEvent);

        var createdIds = new HashSet<Guid>();

        // Act
        for (var i = 0; i < totalSeats; i++)
        {
            var result = await service.CreateBookingAsync(existingEvent.Id, ct);
            createdIds.Add(result.ID);
        }

        // Assert
        createdIds.Should().HaveCount(totalSeats);
        createdIds.Select(r => r).Distinct().Should().HaveCount(totalSeats);
        existingEvent.AvailableSeats.Should().Be(0);
        bookingRepositoryMock.Verify(r => r.AddAsync(It.Is<Booking>(b => b.EventId == existingEvent.Id), ct), Times.Exactly(totalSeats));
        eventRepositoryMock.Verify(r => r.UpdateAsync(existingEvent, ct), Times.Exactly(totalSeats));
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_ShouldThrowNotFound_WhenEventDoesNotExists1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var eventId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, loggerMock.Object, fakeTimeProvider);

        eventRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId, ct))
            .ReturnsAsync((Event) null!);

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventId, ct);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        bookingRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        eventRepositoryMock.Verify(r => r.GetByIdAsync(eventId, ct), Times.Once);
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_ShouldThrowIfCancelled1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, loggerMock.Object, fakeTimeProvider);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(Guid.NewGuid(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        bookingRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task GetBookingByIdAsync_GetByNotExistedID_ShouldThrow_NotFound1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, loggerMock.Object, fakeTimeProvider);

        var nonExistentId = Guid.NewGuid();
        var ct = CancellationToken.None;

        bookingRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentId, ct))
            .ReturnsAsync((Booking?) null);

        // Act
        Func<Task> act = async () => await service.GetBookingByIdAsync(nonExistentId, ct);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        bookingRepositoryMock.Verify(r => r.GetByIdAsync(nonExistentId, ct), Times.Once);
    }


    [Fact]
    [Trait("Category", "GetBooking")]
    public async Task GetBookingByIdAsync_ShouldGetFromRepository1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var ct = CancellationToken.None;
        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, loggerMock.Object, fakeTimeProvider);
        var booking = Booking.Create(Guid.NewGuid(), now);
        bookingRepositoryMock.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        // Act
        var result = await service.GetBookingByIdAsync(booking.Id, ct);

        // Assert
        result.Should().NotBeNull();
        result.ID.Should().Be(booking.Id);
        bookingRepositoryMock.Verify(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "GetBooking")]
    public async Task GetBookingByIdAsync_ShouldThrowIfCancelled1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, loggerMock.Object, fakeTimeProvider);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        bookingRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()));

        // Act
        var act = async () => await service.GetBookingByIdAsync(Guid.NewGuid(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        bookingRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "GetBooking")]
    public async Task ExecuteAsync_ShouldUpdateStatus_AndServiceShouldReturnUpdatedBooking()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerBgMock = new Mock<ILogger<BookingBackgroundService>>();
        var loggerSvcMock = new Mock<ILogger<BookingService>>();
        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero));
        var now = fakeTimeProvider.GetUtcNow().UtcDateTime;

        scopeFactoryMock.Setup(s => s.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var bookingService = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, loggerSvcMock.Object, fakeTimeProvider);

        serviceProviderMock.Setup(s => s.GetService(typeof(IBookingService))).Returns(bookingService);

        var existingEvent = Event.Create("Супермега событие", now, now.AddHours(1), 10);
        var booking = Booking.Create(existingEvent.Id, now);

        eventRepositoryMock.Setup(r => r.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existingEvent);
        bookingRepositoryMock.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var updateFinished = new TaskCompletionSource<bool>();
        bookingRepositoryMock.Setup(r => r.GetAll(BookingStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { booking });

        bookingRepositoryMock
            .Setup(r => r.UpdateAsync(It.Is<Booking>(b => b.Status == BookingStatus.Confirmed), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => updateFinished.TrySetResult(true));

        using var backgroundService = new BookingBackgroundService(scopeFactoryMock.Object, loggerBgMock.Object, fakeTimeProvider);
        using var cts = new CancellationTokenSource();

        // Act
        await backgroundService.StartAsync(cts.Token);

        await Task.Delay(100, cts.Token);

        // Прокручиваем время: 2с (Delay в сервисе) + 1с запас
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(3));

        var completedTask = await Task.WhenAny(updateFinished.Task, Task.Delay(5000, cts.Token));

        if (completedTask != updateFinished.Task)
        {
            // Если таймаут — проверим, не было ли ошибок в логгере
            loggerBgMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                ((Func<It.IsAnyType, Exception, string>) It.IsAny<object>())!), Times.Never(), "В фоновом сервисе произошла ошибка, проверьте логи!");

            throw new Exception("Таймаут: UpdateAsync не вызван.");
        }

        await cts.CancelAsync();
        await backgroundService.StopAsync(CancellationToken.None);

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }


    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBooking_ShouldDecrementSeats_AndReturnDto()
    {
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerSvcMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, loggerSvcMock.Object, fakeTimeProvider);
        var eventId = Guid.NewGuid();
        var totalSeats = 1;
        var ct = CancellationToken.None;
        var myEvent = Event.Create("Концерт", now, now.AddHours(1), totalSeats);
        var initialSeats = myEvent.AvailableSeats;

        eventRepositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(myEvent);

        // Act
        var result = await service.CreateBookingAsync(eventId, ct);

        // Assert
        result.Should().NotBeNull();
        result.EventID.Should().Be(eventId);
        myEvent.AvailableSeats.Should().Be(initialSeats - 1);

        bookingRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        eventRepositoryMock.Verify(r => r.UpdateAsync(myEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_AfterLimitIsReached()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        var ct = CancellationToken.None;
        var totalSeats = 2;
        var eventId = Guid.NewGuid();

        var existingEvent = Event.Create("Новое суперсобытие 4", now, now.AddHours(1), totalSeats);

        eventRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), ct))
            .ReturnsAsync(existingEvent);

        //Заполняем все доступные места
        for (var i = 0; i < totalSeats; i++)
        {
            await service.CreateBookingAsync(eventId, ct);
        }

        // Убеждаемся, что мест действительно не осталось
        existingEvent.AvailableSeats.Should().Be(0);

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventId, ct);

        // Assert
        await act.Should().ThrowAsync<NoAvailableSeatsException>();

        bookingRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Exactly(totalSeats));

        eventRepositoryMock.Verify(
            r => r.UpdateAsync(existingEvent, It.IsAny<CancellationToken>()),
            Times.Exactly(totalSeats));
    }

    [Fact]
    [Trait("Category", "ProcessBooking")]
    public async Task ProcessBookingAsync_ShouldRestoreAvailableSeats_WhenBookingIsRejected()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, loggerMock.Object, fakeTimeProvider);

        var @event = Event.Create("Event", now, now.AddHours(1), 10);
        var booking = Booking.Create(@event.Id, now);

        bookingRepositoryMock
            .Setup(r => r.GetAll(BookingStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { booking });

        // Имитируем задержку в репозитории, чтобы успеть "выстрелить" отменой
        eventRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid id, CancellationToken ct) =>
            {
                await Task.Delay(100, ct); // Ждем отмены
                return @event;
            });

        using var cts = new CancellationTokenSource();

        // Act
        var task = service.UpdateBookingAsync(cts.Token);

        // Даем коду проскочить первую проверку и зайти в ProcessBookingAsync
        await Task.Delay(20);
        cts.Cancel(); // Отменяем ТЕПЕРЬ

        // Assert
        Func<Task> act = async () => await task;
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Теперь статус БУДЕТ Rejected, так как мы попали в catch внутри ProcessBookingAsync
        booking.Status.Should().Be(BookingStatus.Rejected);
    }

    [Fact]
    [Trait("Category", "ProcessBooking")]
    public async Task AfterReject_ShouldAllowToCreateNewBooking_ForSameSeat()
    {
        // Arrange
        var bookingRepoMock = new Mock<IBookingRepository>();
        var eventRepoMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var service = new BookingService(bookingRepoMock.Object, eventRepoMock.Object, loggerMock.Object, fakeTimeProvider);

        var ct = CancellationToken.None;

        var existingEvent = Event.Create("Классное событие", now, now.AddHours(1), 1);

        eventRepoMock.Setup(r => r.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        var pendingBooking = Booking.Create(existingEvent.Id, now);
        bookingRepoMock.Setup(r => r.GetAll(BookingStatus.Pending, ct))
            .ReturnsAsync(new List<Booking> { pendingBooking });

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        try { await service.UpdateBookingAsync(cts.Token); } catch { }

        // Assert
        existingEvent.AvailableSeats.Should().Be(1);
        var newBookingResult = await service.CreateBookingAsync(existingEvent.Id, ct);
        newBookingResult.Should().NotBeNull();
        existingEvent.AvailableSeats.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "ProcessBooking")]
    public async Task CreateBookingAsync_OverbookingProtection_ShouldAllowOnlyLimit()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        var totalSeats = 5;
        var totalRequests = 20;
        var ct = CancellationToken.None;

        var existingEvent = Event.Create("Лучшая шаурма на районе", now, now.AddHours(1), totalSeats);

        eventRepositoryMock
            .Setup(r => r.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act
        var tasks = Enumerable.Range(0, totalRequests)
            .Select(_ => service.CreateBookingAsync(existingEvent.Id, ct))
            .ToList();

        var results = new List<BookingInfoDTO>();
        var exceptions = new List<Exception>();

        foreach (var task in tasks)
        {
            try
            {
                results.Add(await task);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        // Assert
        results.Should().HaveCount(totalSeats);

        exceptions.Should().HaveCount(totalRequests - totalSeats);
        exceptions.All(e => e is NoAvailableSeatsException).Should().BeTrue();

        existingEvent.AvailableSeats.Should().Be(0);

        results.Select(r => r.ID).Distinct().Should().HaveCount(totalSeats);

        eventRepositoryMock.Verify(r => r.UpdateAsync(existingEvent, It.IsAny<CancellationToken>()), Times.Exactly(totalSeats));
        bookingRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Exactly(totalSeats));
    }

    [Fact]
    [Trait("Category", "ProcessBooking")]
    public async Task CreateBookingAsync_ConcurrentRequests_ShouldGenerateUniqueIds()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        var totalSeats = 10;
        var ct = CancellationToken.None;

        // Создаем событие, где мест ровно столько, сколько запросов
        var existingEvent = Event.Create("Лучшая шаурма на районе", now, now.AddHours(1), totalSeats);

        eventRepositoryMock
            .Setup(r => r.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act
        var tasks = Enumerable.Range(0, totalSeats)
            .Select(_ => service.CreateBookingAsync(existingEvent.Id, ct))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Length.Should().Be(totalSeats);

        var uniqueIdsCount = results.Select(r => r.ID).Distinct().Count();
        uniqueIdsCount.Should().Be(totalSeats);

        existingEvent.AvailableSeats.Should().Be(0);

        bookingRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Exactly(totalSeats));

        eventRepositoryMock.Verify(
            r => r.UpdateAsync(existingEvent, It.IsAny<CancellationToken>()),
            Times.Exactly(totalSeats));
    }

}
