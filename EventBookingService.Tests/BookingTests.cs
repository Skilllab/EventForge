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
        booking.Status.Should().Be(BookingStatus.Pending); // Проверка статуса по умолчанию
        booking.ProcessedAt.Should().BeNull(); // По умолчанию время обработки пустое
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

    [Fact]
    public void Status_ShouldBeMutable_WhenUpdated()
    {
        // Arrange
        var booking = Booking.Create(Guid.NewGuid(), DateTime.UtcNow);
        var processedAt = DateTime.UtcNow;

        // Act
        booking.Status = BookingStatus.Confirmed;
        booking.ProcessedAt = processedAt;

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().Be(processedAt);
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
