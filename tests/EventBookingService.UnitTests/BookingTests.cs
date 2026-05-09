using EventBookingService.Domain.Entities;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace EventBookingService.UnitTests;

public class BookingTests
{
    [Fact]
    public void Create_ShouldInitializeFieldsCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var booking = Booking.Create(eventId, createdAt);

        // Assert
        booking.Id.Should().NotBeEmpty();
        booking.EventId.Should().Be(eventId);
        booking.CreatedAt.Should().Be(createdAt);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldAlwaysGenerateUniqueIds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var booking1 = Booking.Create(eventId, createdAt);
        var booking2 = Booking.Create(eventId, createdAt);

        // Assert
        booking1.Id.Should().NotBe(booking2.Id);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    public void BookingStatus_ShouldSupportAllEnumValues(BookingStatus status)
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), DateTime.UtcNow);

        // Act
        booking.Status = status;

        // Assert
        booking.Status.Should().Be(status);
    }

    [Fact]
    public void BookingStatus_ShouldSetStatusToConfirmed_AndFillProcessedAt()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var booking = Booking.Create(eventId, now);


        // Начальное состояние
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();

        // Act
        booking.Confirm(now);

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);

        booking.ProcessedAt.Should().NotBeNull();

        booking.ProcessedAt.Value.Should().BeCloseTo(now, TimeSpan.FromSeconds(3));

        booking.CreatedAt.Should().Be(now);
        booking.EventId.Should().Be(eventId);
    }

    [Fact]
    public void BookingStatus_ShouldSetStatusToRejected_AndFillProcessedAt()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var booking = Booking.Create(eventId, now);


        // Act
        booking.Reject(now);

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);

        booking.ProcessedAt.Should().NotBeNull();

        booking.ProcessedAt.Value.Should().BeCloseTo(now, TimeSpan.FromSeconds(2));
    }
}
