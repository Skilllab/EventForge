using EventBookingService.Application.Common;
using EventBookingService.Application.DTO;
using EventBookingService.Application.Interfaces;
using EventBookingService.Application.Services;
using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Moq;

namespace EventBookingService.UnitTests;

public class BookingServiceTests
{
    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_ShouldSaveBooking_WhenEventExists1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var userId = Guid.NewGuid();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var existingEvent = Event.Create("Свежее тестовое событие", now, now.AddHours(1), 1);

        // Mock для GetByIdWithLockInContextAsync
        var transactionContextMock = new Mock<ITransactionContext>();

        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(It.IsAny<Guid>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(existingEvent.Id, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync(existingEvent);

        // Mock для ExecuteAsync - вызываем операцию внутри мока
        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        var result = await service.CreateBookingAsync(existingEvent.Id, userId, ct);

        // Assert
        result.Status.Should().Be(nameof(BookingStatus.Pending));
        bookingRepositoryMock.Verify(r => r.AddInContextAsync(It.Is<Booking>(b => b.EventId == existingEvent.Id), It.IsAny<ITransactionContext>(), ct), Times.Once);
        eventRepositoryMock.Verify(r => r.UpdateInContextAsync(It.Is<Event>(e => e.AvailableSeats == 0), It.IsAny<ITransactionContext>(), ct), Times.Once);
        eventRepositoryMock.Verify(r => r.GetByIdWithLockInContextAsync(existingEvent.Id, It.IsAny<ITransactionContext>(), ct), Times.Once);
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_RunManyTimes_ShouldSaveBooking_WhenEventExists1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();

        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var ct = CancellationToken.None;
        var totalSeats = 10;
        var userId = Guid.NewGuid();
        var existingEvent = Event.Create("Свежее тестовое событие 2", now, now.AddHours(1), totalSeats);
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });

        var transactionContextMock = new Mock<ITransactionContext>();

        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(It.IsAny<Guid>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(existingEvent.Id, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync(existingEvent);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, userRepositoryMock.Object, bookingOptions, transactionServiceMock.Object, loggerMock.Object, fakeTimeProvider);

        var createdIds = new HashSet<Guid>();

        // Act
        for (var i = 0; i < totalSeats; i++)
        {
            var result = await service.CreateBookingAsync(existingEvent.Id, userId, ct);
            createdIds.Add(result.ID);
        }

        // Assert
        createdIds.Should().HaveCount(totalSeats);
        createdIds.Select(r => r).Distinct().Should().HaveCount(totalSeats);
        existingEvent.AvailableSeats.Should().Be(0);
        bookingRepositoryMock.Verify(r => r.AddInContextAsync(It.Is<Booking>(b => b.EventId == existingEvent.Id), It.IsAny<ITransactionContext>(), ct), Times.Exactly(totalSeats));
        eventRepositoryMock.Verify(r => r.UpdateInContextAsync(existingEvent, It.IsAny<ITransactionContext>(), ct), Times.Exactly(totalSeats));
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_ShouldThrowNotFound_WhenEventDoesNotExists1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var eventId = Guid.NewGuid();
        var ct = CancellationToken.None;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });

        var transactionContextMock = new Mock<ITransactionContext>();

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(eventId, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync((Event) null!);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, userRepositoryMock.Object, bookingOptions, transactionServiceMock.Object, loggerMock.Object, fakeTimeProvider);

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventId, Guid.NewGuid(), ct);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        bookingRepositoryMock.Verify(r => r.AddInContextAsync(It.IsAny<Booking>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()), Times.Never);
        eventRepositoryMock.Verify(r => r.GetByIdWithLockInContextAsync(eventId, It.IsAny<ITransactionContext>(), ct), Times.Once);
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_ShouldThrowIfCancelled1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });

        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, userRepositoryMock.Object, bookingOptions, transactionServiceMock.Object, loggerMock.Object, fakeTimeProvider);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        bookingRepositoryMock.Verify(r => r.AddInContextAsync(It.IsAny<Booking>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "GetBooking")]
    public async Task GetBookingByIdAsync_GetByNotExistedID_ShouldThrow_NotFound1()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, userRepositoryMock.Object, bookingOptions, transactionServiceMock.Object, loggerMock.Object, fakeTimeProvider);

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
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var ct = CancellationToken.None;
        var userId = Guid.NewGuid();
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, userRepositoryMock.Object, bookingOptions, transactionServiceMock.Object, loggerMock.Object, fakeTimeProvider);
        var booking = Booking.Create(Guid.NewGuid(), userId, now);
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
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, userRepositoryMock.Object, bookingOptions, transactionServiceMock.Object, loggerMock.Object, fakeTimeProvider);
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
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });

        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, userRepositoryMock.Object, bookingOptions, transactionServiceMock.Object, loggerMock.Object, fakeTimeProvider);

        var userId = Guid.NewGuid();
        var existingEvent = Event.Create("Супермега событие", now, now.AddHours(1), 10);
        var booking = Booking.Create(existingEvent.Id, userId, now);

        bookingRepositoryMock.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        // Act
        var result = await service.GetBookingByIdAsync(booking.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ID.Should().Be(booking.Id);
        result.Status.Should().Be(nameof(BookingStatus.Pending));
    }


    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_ShouldDecrementSeats_AndReturnDto()
    {
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerSvcMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var totalSeats = 1;
        var ct = CancellationToken.None;
        var myEvent = Event.Create("Концерт", now, now.AddHours(1), totalSeats);
        var initialSeats = myEvent.AvailableSeats;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });

        var transactionContextMock = new Mock<ITransactionContext>();

        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(It.IsAny<Guid>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        eventRepositoryMock.Setup(r => r.GetByIdWithLockInContextAsync(eventId, It.IsAny<ITransactionContext>(), ct)).ReturnsAsync(myEvent);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(bookingRepositoryMock.Object, eventRepositoryMock.Object, userRepositoryMock.Object, bookingOptions, transactionServiceMock.Object, loggerSvcMock.Object, fakeTimeProvider);

        // Act
        var result = await service.CreateBookingAsync(eventId, userId, ct);

        // Assert
        result.Should().NotBeNull();
        result.EventID.Should().Be(eventId);
        myEvent.AvailableSeats.Should().Be(initialSeats - 1);

        bookingRepositoryMock.Verify(r => r.AddInContextAsync(It.IsAny<Booking>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()), Times.Once);
        eventRepositoryMock.Verify(r => r.UpdateInContextAsync(myEvent, It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_AfterLimitIsReached()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var ct = CancellationToken.None;
        var totalSeats = 2;
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });

        var existingEvent = Event.Create("Новое суперсобытие 4", now, now.AddHours(1), totalSeats);

        var transactionContextMock = new Mock<ITransactionContext>();

        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(It.IsAny<Guid>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(eventId, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync(existingEvent);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        for (var i = 0; i < totalSeats; i++) 
            await service.CreateBookingAsync(eventId, userId, ct);

        existingEvent.AvailableSeats.Should().Be(0);

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(eventId, userId, ct);

        // Assert
        await act.Should().ThrowAsync<NoAvailableSeatsException>();

        bookingRepositoryMock.Verify(
            r => r.AddInContextAsync(It.IsAny<Booking>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()),
            Times.Exactly(totalSeats));

        eventRepositoryMock.Verify(
            r => r.UpdateInContextAsync(existingEvent, It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()),
            Times.Exactly(totalSeats));
    }

    [Fact]
    [Trait("Category", "ProcessBooking")]
    public async Task ProcessBookingAsync_ShouldRestoreAvailableSeats_WhenBookingIsRejected()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var userId = Guid.NewGuid();
        var @event = Event.Create("Event", now, now.AddHours(1), 10);
        @event.TryReserveSeats();

        var booking = Booking.Create(@event.Id, userId, now);

        var transactionContextMock = new Mock<ITransactionContext>();

        bookingRepositoryMock
            .Setup(r => r.GetAllAsync(BookingStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { booking });

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(@event.Id, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync(@event);

        var logInfoCallCount = 0;
        loggerMock
            .Setup(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("подтверждена")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() =>
            {
                logInfoCallCount++;
                if (logInfoCallCount == 1)
                    throw new InvalidOperationException("Simulated error after Confirm");
            });

        Exception? caughtException = null;
        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task>>(), ct))
            .Returns(async (Func<ITransactionContext, Task> operation, CancellationToken cancellationToken) =>
            {
                try
                {
                    await operation(transactionContextMock.Object);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                    // Проглатываем только для теста
                }
            });

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        var initialAvailableSeats = @event.AvailableSeats; // 9

        // Act
        await service.UpdateBookingAsync(ct);

        // Assert
        caughtException.Should().NotBeNull();

        booking.Status.Should().Be(BookingStatus.Rejected, 
            "бронь должна быть отклонена в блоке catch после ошибки");

        @event.AvailableSeats.Should().Be(initialAvailableSeats + 1, 
            "места должны быть восстановлены после ошибки");

        eventRepositoryMock.Verify(
            r => r.UpdateInContextAsync(@event, It.IsAny<ITransactionContext>(), ct),
            Times.Once,
            "события должно быть сохранено после восстановления мест");

        bookingRepositoryMock.Verify(
            r => r.UpdateInContextAsync(
                It.Is<Booking>(b => b.Status == BookingStatus.Rejected), 
                It.IsAny<ITransactionContext>(), 
                ct),
            Times.Once,
            "отклоненная бронь должна быть сохранена в finally");
    }

    [Fact]
    [Trait("Category", "ProcessBooking")]
    public async Task AfterReject_ShouldAllowToCreateNewBooking_ForSameSeat()
    {
        // Arrange
        var bookingRepoMock = new Mock<IBookingRepository>();
        var eventRepoMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });

        var service = new BookingService(bookingRepoMock.Object, eventRepoMock.Object, userRepositoryMock.Object, bookingOptions, transactionServiceMock.Object, loggerMock.Object, fakeTimeProvider);

        var ct = CancellationToken.None;
        var userId = Guid.NewGuid();

        var existingEvent = Event.Create("Классное событие", now, now.AddHours(1), 1);

        eventRepoMock.Setup(r => r.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        var transactionContextMock = new Mock<ITransactionContext>();

        bookingRepoMock
            .Setup(r => r.GetUserBookingInContextAsync(It.IsAny<Guid>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        eventRepoMock
            .Setup(r => r.GetByIdWithLockInContextAsync(existingEvent.Id, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync(existingEvent);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var bookingUserId = Guid.NewGuid();
        var pendingBooking = Booking.Create(existingEvent.Id, bookingUserId, now);
        bookingRepoMock.Setup(r => r.GetAllAsync(BookingStatus.Pending, ct))
            .ReturnsAsync(new List<Booking> { pendingBooking });

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        try { await service.UpdateBookingAsync(cts.Token); } catch { }

        // Assert
        existingEvent.AvailableSeats.Should().Be(1);
        var newBookingResult = await service.CreateBookingAsync(existingEvent.Id, userId, ct);
        newBookingResult.Should().NotBeNull();
        existingEvent.AvailableSeats.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_OverbookingProtection_ShouldAllowOnlyLimit()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var totalSeats = 5;
        var totalRequests = 20;
        var ct = CancellationToken.None;
        var userId = Guid.NewGuid();

        var existingEvent = Event.Create("Лучшая шаурма на районе", now, now.AddHours(1), totalSeats);
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });

        var transactionContextMock = new Mock<ITransactionContext>();

        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(It.IsAny<Guid>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(existingEvent.Id, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync(existingEvent);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        var tasks = Enumerable.Range(0, totalRequests)
            .Select(_ => service.CreateBookingAsync(existingEvent.Id, userId, ct))
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

        eventRepositoryMock.Verify(r => r.UpdateInContextAsync(existingEvent, It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()), Times.Exactly(totalSeats));
        bookingRepositoryMock.Verify(r => r.AddInContextAsync(It.IsAny<Booking>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()), Times.Exactly(totalSeats));
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_ConcurrentRequests_ShouldGenerateUniqueIds()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var totalSeats = 10;
        var ct = CancellationToken.None;
        var userId = Guid.NewGuid();

        var existingEvent = Event.Create("Лучшая шаурма на районе", now, now.AddHours(1), totalSeats);
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });

        var transactionContextMock = new Mock<ITransactionContext>();

        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(It.IsAny<Guid>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(existingEvent.Id, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync(existingEvent);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        var tasks = Enumerable.Range(0, totalSeats)
            .Select(_ => service.CreateBookingAsync(existingEvent.Id, userId, ct))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Length.Should().Be(totalSeats);

        var uniqueIdsCount = results.Select(r => r.ID).Distinct().Count();
        uniqueIdsCount.Should().Be(totalSeats);

        existingEvent.AvailableSeats.Should().Be(0);

        bookingRepositoryMock.Verify(
            r => r.AddInContextAsync(It.IsAny<Booking>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()),
            Times.Exactly(totalSeats));

        eventRepositoryMock.Verify(
            r => r.UpdateInContextAsync(existingEvent, It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()),
            Times.Exactly(totalSeats));
    }

    [Fact]
    [Trait("Category", "BusinessRules")]
    public async Task CreateBookingAsync_ShouldThrowBookingPastEventException_WhenEventHasAlreadyStarted()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var userId = Guid.NewGuid();
        var pastEvent = Event.Create("Прошедшее событие", now.AddHours(-2), now.AddHours(-1), 10);

        var transactionContextMock = new Mock<ITransactionContext>();

        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(It.IsAny<Guid>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(pastEvent.Id, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync(pastEvent);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(pastEvent.Id, userId, ct);

        // Assert
        await act.Should().ThrowAsync<BookingPastEventException>();
        bookingRepositoryMock.Verify(
            r => r.AddInContextAsync(It.IsAny<Booking>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "BusinessRules")]
    public async Task CreateBookingAsync_ShouldSucceed_WhenEventIsInFuture()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var userId = Guid.NewGuid();
        var futureEvent = Event.Create("Будущее событие", now.AddHours(1), now.AddHours(2), 10);

        var transactionContextMock = new Mock<ITransactionContext>();
    
        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(It.IsAny<Guid>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(futureEvent.Id, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync(futureEvent);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        var result = await service.CreateBookingAsync(futureEvent.Id, userId, ct);

        // Assert
        result.Should().NotBeNull();
        result.EventID.Should().Be(futureEvent.Id);
        result.Status.Should().Be(nameof(BookingStatus.Pending));
        bookingRepositoryMock.Verify(
            r => r.AddInContextAsync(It.Is<Booking>(b => b.EventId == futureEvent.Id), It.IsAny<ITransactionContext>(), ct),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "BusinessRules")]
    public async Task CreateBookingAsync_ShouldThrowBookingLimitExceededException_WhenUserExceedsMaxBookingCount()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var maxBookingCount = 3;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = maxBookingCount });
        var ct = CancellationToken.None;

        var userId = Guid.NewGuid();
        var @event = Event.Create("Событие", now, now.AddHours(1), 10);

        var existingBookings = Enumerable.Range(0, maxBookingCount + 1)
            .Select(_ => Booking.Create(Guid.NewGuid(), userId, now))
            .ToList();

        var transactionContextMock = new Mock<ITransactionContext>();

        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(userId, It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBookings);

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(@event.Id, It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync(@event);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        Func<Task> act = async () => await service.CreateBookingAsync(@event.Id, userId, ct);

        // Assert
        await act.Should().ThrowAsync<BookingLimitExceededException>();
        bookingRepositoryMock.Verify(
            r => r.AddInContextAsync(It.IsAny<Booking>(), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "BusinessRules")]
    public async Task CreateBookingAsync_UserLimits_ShouldNotAffectEachOther()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var maxBookingCount = 2;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = maxBookingCount });
        var ct = CancellationToken.None;

        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var @event1 = Event.Create("Популярное событие 1", now, now.AddHours(1), 100);
        var @event2 = Event.Create("Популярное событие 2", now, now.AddHours(2), 100);

        var transactionContextMock = new Mock<ITransactionContext>();

        var user1Bookings = Enumerable.Range(0, maxBookingCount + 1)
            .Select(_ => Booking.Create(Guid.NewGuid(), userId1, now))
            .ToList();

        // User2 не имеет броней
        var user2Bookings = new List<Booking>();

        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(userId1, It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user1Bookings);

        bookingRepositoryMock
            .Setup(r => r.GetUserBookingInContextAsync(userId2, It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user2Bookings);

        eventRepositoryMock
            .Setup(r => r.GetByIdWithLockInContextAsync(It.IsAny<Guid>(), It.IsAny<ITransactionContext>(), ct))
            .ReturnsAsync((Guid id, ITransactionContext ctx, CancellationToken token) =>
                id == @event1.Id ? @event1 : @event2);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<BookingInfoDTO>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<BookingInfoDTO>> operation, CancellationToken cancellationToken) => await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        Func<Task> act1 = async () => await service.CreateBookingAsync(@event1.Id, userId1, ct);

        var resultUser2 = await service.CreateBookingAsync(@event2.Id, userId2, ct);

        // Assert
        await act1.Should().ThrowAsync<BookingLimitExceededException>();
        resultUser2.Should().NotBeNull();
        resultUser2.Status.Should().Be(nameof(BookingStatus.Pending));

        bookingRepositoryMock.Verify(
            r => r.AddInContextAsync(It.Is<Booking>(b => b.UserId == userId1), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()),
            Times.Never);

        bookingRepositoryMock.Verify(
            r => r.AddInContextAsync(It.Is<Booking>(b => b.UserId == userId2), It.IsAny<ITransactionContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #region CancelBooking Tests

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_ShouldSucceed_WhenBookingIsPending()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var userId = Guid.NewGuid();
        var userLogin = "Тестировщик";
        var user = User.Create(userLogin, "password_hash", RoleType.User);
        typeof(User).GetProperty("Id")!.SetValue(user, userId);

        var @event = Event.Create("Открытие новой фабрики", now, now.AddHours(1), 10);
        var booking = Booking.Create(@event.Id, userId, now);

        var transactionContextMock = new Mock<ITransactionContext>();

        userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        bookingRepositoryMock.Setup(r => r.GetByIdAsync(booking.Id, ct)).ReturnsAsync(booking);
        eventRepositoryMock.Setup(r => r.GetByIdWithLockInContextAsync(@event.Id, It.IsAny<ITransactionContext>(), ct)).ReturnsAsync(@event);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<bool>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<bool>> operation, CancellationToken cancellationToken) =>
                await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        var result = await service.CancelBooking(booking.Id, userId, ct);

        // Assert
        result.Should().BeTrue();
        bookingRepositoryMock.Verify(r => r.UpdateInContextAsync(
            It.Is<Booking>(b => b.Id == booking.Id && b.Status == BookingStatus.Cancelled),
            It.IsAny<ITransactionContext>(),
            ct), Times.Once);
        eventRepositoryMock.Verify(r => r.UpdateInContextAsync(
            It.Is<Event>(e => e.Id == @event.Id),
            It.IsAny<ITransactionContext>(),
            ct), Times.Once);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_ShouldThrow_WhenBookingAlreadyCancelled()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var userLogin = "Тестировщик";
        var user = User.Create(userLogin, "password_hash", RoleType.User);

        var @event = Event.Create("Открытие другой новой фабрики", now, now.AddHours(1), 10);
        var booking = Booking.Create(@event.Id, user.Id, now);
        booking.Cancel(now); // Уже отменено

        userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        bookingRepositoryMock.Setup(r => r.GetByIdAsync(booking.Id, ct)).ReturnsAsync(booking);

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        Func<Task> act = async () => await service.CancelBooking(booking.Id, user.Id, ct);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        transactionServiceMock.Verify(
            ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_ShouldThrow_WhenBookingIsRejected()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var userLogin = "Тестировщик";
        var user = User.Create(userLogin, "password_hash", RoleType.User);

        var @event = Event.Create("Закрытие новой фабрики", now, now.AddHours(1), 10);
        var booking = Booking.Create(@event.Id, user.Id, now);
        booking.Reject(now);

        userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        bookingRepositoryMock.Setup(r => r.GetByIdAsync(booking.Id, ct)).ReturnsAsync(booking);

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        Func<Task> act = async () => await service.CancelBooking(booking.Id, user.Id, ct);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        transactionServiceMock.Verify(
            ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_ShouldSucceed_WhenBookingIsConfirmed()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var userLogin = "Тестировщик";
        var user = User.Create(userLogin, "password_hash", RoleType.User);

        var @event = Event.Create("Опять что-то с фабрикой", now, now.AddHours(1), 10);
        var booking = Booking.Create(@event.Id, user.Id, now);
        booking.Confirm(now); // Подтверждено

        var transactionContextMock = new Mock<ITransactionContext>();

        userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        bookingRepositoryMock.Setup(r => r.GetByIdAsync(booking.Id, ct)).ReturnsAsync(booking);
        eventRepositoryMock.Setup(r => r.GetByIdWithLockInContextAsync(@event.Id, It.IsAny<ITransactionContext>(), ct)).ReturnsAsync(@event);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<bool>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<bool>> operation, CancellationToken cancellationToken) =>
                await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        var result = await service.CancelBooking(booking.Id, user.Id, ct);

        // Assert
        result.Should().BeTrue();
        bookingRepositoryMock.Verify(r => r.UpdateInContextAsync(
            It.Is<Booking>(b => b.Id == booking.Id && b.Status == BookingStatus.Cancelled),
            It.IsAny<ITransactionContext>(),
            ct), Times.Once);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var userLogin = "Тестировщик12";
        userRepositoryMock.Setup(r => r.GetByLoginAsync(userLogin)).ReturnsAsync((User?) null);

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        Func<Task> act = async () => await service.CancelBooking(Guid.NewGuid(), Guid.NewGuid(), ct);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        bookingRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_ShouldThrow_WhenBookingNotFound()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var userId = Guid.NewGuid();
        var userLogin = "Тестировщик";
        var user = User.Create(userLogin, "password_hash", RoleType.User);
        typeof(User).GetProperty("Id")!.SetValue(user, userId);

        var bookingId = Guid.NewGuid();

        userRepositoryMock.Setup(r => r.GetByLoginAsync(userLogin)).ReturnsAsync(user);
        bookingRepositoryMock.Setup(r => r.GetByIdAsync(bookingId, ct)).ReturnsAsync((Booking?) null);

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        Func<Task> act = async () => await service.CancelBooking(bookingId, userId, ct);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        transactionServiceMock.Verify(
            ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_ShouldThrow_WhenUserHasNoPermission()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var owner = User.Create("owner", "hash", RoleType.User);
        var otherUser = User.Create("Тестировщик", "hash", RoleType.User);
        var @event = Event.Create("И снова фабрика", now, now.AddHours(1), 10);
        var booking = Booking.Create(@event.Id, owner.Id, now);

        userRepositoryMock.Setup(r => r.GetByIdAsync(otherUser.Id)).ReturnsAsync(otherUser);
        bookingRepositoryMock.Setup(r => r.GetByIdAsync(booking.Id, ct)).ReturnsAsync(booking);

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        Func<Task> act = async () => await service.CancelBooking(booking.Id, otherUser.Id, ct);

        // Assert
        await act.Should().ThrowAsync<InsufficientPermissionsException>();
        transactionServiceMock.Verify(
            ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_ShouldSucceed_WhenUserIsAdmin()
    {
        // Arrange
        var bookingRepositoryMock = new Mock<IBookingRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var transactionServiceMock = new Mock<ITransactionService>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var bookingOptions = Options.Create(new BookingOptions { MaxBookingCount = 5 });
        var ct = CancellationToken.None;

        var adminLogin = "Тестировщик";
        var admin = User.Create(adminLogin, "hash", RoleType.Admin);

        var @event = Event.Create("Заводы будут запущены", now, now.AddHours(1), 10);
        var booking = Booking.Create(@event.Id, admin.Id, now);

        var transactionContextMock = new Mock<ITransactionContext>();

        userRepositoryMock.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);
        bookingRepositoryMock.Setup(r => r.GetByIdAsync(booking.Id, ct)).ReturnsAsync(booking);
        eventRepositoryMock.Setup(r => r.GetByIdWithLockInContextAsync(@event.Id, It.IsAny<ITransactionContext>(), ct)).ReturnsAsync(@event);

        transactionServiceMock
            .Setup(ts => ts.ExecuteAsync(It.IsAny<Func<ITransactionContext, Task<bool>>>(), ct))
            .Returns(async (Func<ITransactionContext, Task<bool>> operation, CancellationToken cancellationToken) =>
                await operation(transactionContextMock.Object));

        var service = new BookingService(
            bookingRepositoryMock.Object,
            eventRepositoryMock.Object,
            userRepositoryMock.Object,
            bookingOptions,
            transactionServiceMock.Object,
            loggerMock.Object,
            fakeTimeProvider);

        // Act
        var result = await service.CancelBooking(booking.Id, admin.Id, ct);

        // Assert
        result.Should().BeTrue();
        bookingRepositoryMock.Verify(r => r.UpdateInContextAsync(
            It.Is<Booking>(b => b.Id == booking.Id && b.Status == BookingStatus.Cancelled),
            It.IsAny<ITransactionContext>(),
            ct), Times.Once);
    }

    #endregion

}

