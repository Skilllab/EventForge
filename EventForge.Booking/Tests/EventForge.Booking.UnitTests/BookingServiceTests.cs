using System.Text.Json;

using EventForge.Booking.Application.Common;
using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Application.Services;
using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Domain.Exceptions;
using EventForge.Contract.Brokers;
using EventForge.Shared.Entities.Enums;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Moq;

namespace EventForge.Booking.UnitTests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_Should_Save_Booking_When_Limit_Not_Exceeded()
    {
        var repositoryMock = new Mock<IBookingRepository>();
        var loggerMock = new Mock<ILogger<BookingService>>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 12, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new BookingOptions { MaxBookingCount = 3 });
        var service = new BookingService(repositoryMock.Object, options, loggerMock.Object, fakeTimeProvider);
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetUserActiveBookingsCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        BookingModel? savedBooking = null;
        repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<BookingModel>(), It.IsAny<CancellationToken>()))
            .Callback<BookingModel, CancellationToken>((booking, _) => savedBooking = booking)
            .Returns(Task.CompletedTask);

        var result = await service.CreateBookingAsync(eventId, userId, CancellationToken.None);

        result.EventID.Should().Be(eventId);
        result.Status.Should().Be(nameof(BookingStatus.Pending));
        savedBooking.Should().NotBeNull();
        savedBooking!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_When_Limit_Exceeded()
    {
        var repositoryMock = new Mock<IBookingRepository>();
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 2 }),
            Mock.Of<ILogger<BookingService>>(),
            TimeProvider.System);
        var userId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetUserActiveBookingsCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        Func<Task> act = () => service.CreateBookingAsync(Guid.NewGuid(), userId, CancellationToken.None);

        await act.Should().ThrowAsync<BookingLimitExceededException>();
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Throw_When_Booking_Not_Found()
    {
        var repositoryMock = new Mock<IBookingRepository>();
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            TimeProvider.System);
        var bookingId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingModel?)null);

        Func<Task> act = () => service.GetBookingByIdAsync(bookingId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CancelBooking_Should_Throw_When_User_Has_No_Permission()
    {
        var repositoryMock = new Mock<IBookingRepository>();
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            TimeProvider.System);
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        repositoryMock
            .Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        Func<Task> act = () => service.CancelBooking(booking.Id, Guid.NewGuid(), RoleType.User, CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientPermissionsException>();
    }

    [Fact]
    public async Task CancelBooking_Should_Save_Cancelled_Outbox_For_Owner()
    {
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var ownerId = Guid.NewGuid();
        var booking = BookingModel.Create(Guid.NewGuid(), ownerId, fakeTimeProvider.GetUtcNow().UtcDateTime);

        repositoryMock
            .Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        OutboxMessage? savedOutbox = null;
        repositoryMock
            .Setup(x => x.CancelAndAddOutboxAsync(booking.Id, ownerId, It.IsAny<DateTime>(), It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, DateTime, OutboxMessage, CancellationToken>((_, _, _, outbox, _) => savedOutbox = outbox)
            .ReturnsAsync(true);

        var result = await service.CancelBooking(booking.Id, ownerId, RoleType.User, CancellationToken.None);

        result.Should().BeTrue();
        savedOutbox.Should().NotBeNull();
        savedOutbox!.Topic.Should().Be(TopicNames.BookingCancelled);
        var payload = JsonSerializer.Deserialize<BookingCancelled>(savedOutbox.Payload);
        payload.Should().NotBeNull();
        payload!.BookingId.Should().Be(booking.Id);
    }

    [Fact]
    public async Task UpdateBookingAsync_Should_Confirm_All_Pending_Bookings()
    {
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 16, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var bookings = new List<BookingModel>
        {
            BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), fakeTimeProvider.GetUtcNow().UtcDateTime),
            BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), fakeTimeProvider.GetUtcNow().UtcDateTime)
        };

        repositoryMock
            .Setup(x => x.GetAllAsync(BookingStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);
        repositoryMock
            .Setup(x => x.ConfirmAndAddOutboxAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await service.UpdateBookingAsync(CancellationToken.None);

        repositoryMock.Verify(x => x.ConfirmAndAddOutboxAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateBookingAsync_Should_Reject_Booking_When_Confirm_Throws()
    {
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 17, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), fakeTimeProvider.GetUtcNow().UtcDateTime);

        repositoryMock
            .Setup(x => x.GetAllAsync(BookingStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync([booking]);
        repositoryMock
            .Setup(x => x.ConfirmAndAddOutboxAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("confirm failed"));
        repositoryMock
            .Setup(x => x.RejectAndAddOutboxAsync(booking.Id, It.IsAny<DateTime>(), It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Func<Task> act = () => service.UpdateBookingAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        repositoryMock.Verify(x => x.RejectAndAddOutboxAsync(booking.Id, It.IsAny<DateTime>(), It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
