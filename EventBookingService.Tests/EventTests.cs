using EventBookingService.Domain.Exceptions;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace EventBookingService.Tests;

/// <summary>
/// Класс тестирования доменной модели
/// </summary>
public class EventTests
{
    [Fact]
    [Trait("Category", "CreateEvent")]
    public void Create_WithValidParameters_ShouldReturnEvent()
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 100;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddDays(2);

        // Act
        var @event = Event.Create(defaultTitle, startDate, endDate, defaultSeats, defaultDescription);

        // Assert
        @event.Id.Should().NotBeEmpty();
        @event.Title.Should().Be(defaultTitle);
        @event.TotalSeats.Should().Be(defaultSeats);
        @event.AvailableSeats.Should().Be(defaultSeats);
        @event.StartAt.Should().Be(startDate);
        @event.EndAt.Should().Be(endDate);
    }

    [Fact]
    [Trait("Category", "CreateEvent")]
    public void Create_WhenEndDateIsBeforeStartDate_ShouldThrowValidationException()
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 100;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddHours(-1);

        // Act
        Action act = () => Event.Create(defaultTitle, startDate, endDate, defaultSeats, defaultDescription);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [Trait("Category", "CreateEvent")]
    public void Create_WhenTotalSeatsIsInvalid_ShouldThrowValidationException(int invalidSeats)
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 100;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddDays(1);

        // Act
        Action act = () => Event.Create(defaultTitle, startDate, endDate, invalidSeats, defaultDescription);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }


    [Fact]
    [Trait("Category", "ReserveSeats")]
    public void TryReserveSeats_WhenSeatsAvailable_ShouldDecreaseAvailableSeatsAndReturnTrue()
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 10;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddDays(1);
        var @event = Event.Create(defaultTitle, startDate, endDate, defaultSeats, defaultDescription);

        // Act
        var result = @event.TryReserveSeats(3);

        // Assert
        result.Should().BeTrue();
        @event.AvailableSeats.Should().Be(7);
    }

    [Fact]
    [Trait("Category", "ReserveSeats")]
    public void TryReserveSeats_WhenNotEnoughSeats_ShouldReturnFalseAndNotChangeCount()
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 5;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddDays(1);
        var @event = Event.Create(defaultTitle, startDate, endDate, defaultSeats, defaultDescription );

        // Act
        var result = @event.TryReserveSeats(6);

        // Assert
        result.Should().BeFalse();
        @event.AvailableSeats.Should().Be(5);
    }

    [Fact]
    [Trait("Category", "ReserveSeats")]
    public void TryReserveSeats_WithNegativeCount_ShouldReturnFalse()
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 5;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddDays(1);
        var @event = Event.Create(defaultTitle, startDate, endDate, defaultSeats, defaultDescription);

        // Act
        var result = @event.TryReserveSeats(-1);

        // Assert
        result.Should().BeFalse();
        @event.AvailableSeats.Should().Be(5);
    }



    [Fact]
    [Trait("Category", "ReleaseSeats")]
    public void ReleaseSeats_ShouldIncreaseAvailableSeats()
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 10;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddDays(1);
        var @event = Event.Create(defaultTitle, startDate, endDate, defaultSeats);
        @event.TryReserveSeats(5);

        // Act
        @event.ReleaseSeats(2);

        // Assert
        @event.AvailableSeats.Should().Be(7);
    }

    [Fact]
    [Trait("Category", "ReleaseSeats")]
    public void ReleaseSeats_WhenExceedingTotalSeats_ShouldCapAtTotalSeats()
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 10;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddDays(1);
        var @event = Event.Create(defaultTitle, startDate, endDate, defaultSeats);
        @event.TryReserveSeats(2);

        // Act
        @event.ReleaseSeats(5);

        // Assert
        @event.AvailableSeats.Should().Be(10);
    }

    [Fact]
    [Trait("Category", "ReleaseSeats")]
    public void ReleaseSeats_WithNegativeCount_ShouldNotChangeAvailableSeats()
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 10;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddDays(1);
        var @event = Event.Create(defaultTitle, startDate, endDate, defaultSeats);
        @event.TryReserveSeats(5);

        // Act
        @event.ReleaseSeats(-5);

        // Assert
        @event.AvailableSeats.Should().Be(5);
    }


    [Fact]
    [Trait("Category", "UpdateEvent")]
    public void UpdateEvent_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 10;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddDays(1);
        var @event = Event.Create(defaultTitle, startDate, endDate, defaultSeats);

        var newTitle = "Не совсем первая и не совсем межпланетная конференция";
        var description = "Тут должно быть описание";
        var newStart = DateTime.Now.AddDays(10);
        var newEnd = DateTime.Now.AddDays(11);

        // Act
        @event.UpdateEvent(newTitle, newStart, newEnd, description);

        // Assert
        @event.Title.Should().Be(newTitle);
        @event.StartAt.Should().Be(newStart);
        @event.EndAt.Should().Be(newEnd);
        @event.Description.Should().Be(description);
    }

    [Fact]
    [Trait("Category", "UpdateEvent")]
    public void UpdateEvent_WithInvalidDates_ShouldThrowValidationException()
    {
        // Arrange
        var defaultTitle = "Межпланетная конференция .NET";
        var defaultSeats = 10;
        var defaultDescription = "Первая в своём роде конференция таких масштабов";
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var startDate = now;
        var endDate = now.AddDays(11);
        var @event = Event.Create(defaultTitle, startDate, endDate, defaultSeats);

        // Act
        Action act = () => @event.UpdateEvent(defaultTitle, endDate, startDate, defaultDescription);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }


}
