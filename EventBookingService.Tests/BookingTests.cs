using EventBookingService.WebAPI.Models.Domain;

using FluentAssertions;

namespace EventBookingService.Tests;

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
}
