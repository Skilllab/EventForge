using EventBookingService.Application.DTO;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace EventBookingService.UnitTests.ModelsTests.Application
{
    public class CreateEventDtoTests
    {
        private readonly DateTime _now;

        public CreateEventDtoTests()
        {
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            _now = fixedUtcNow.UtcDateTime;
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateObjectSuccessfully()
        {
            // Arrange
            var title = "Конференция по .NET";
            var startAt = _now;
            var endAt = _now.AddHours(2);
            var totalSeats = 50;
            var description = "Описание";

            // Act
            var dto = new CreateEventDto(title, startAt, endAt, totalSeats, description);

            // Assert
            dto.Should().NotBeNull();
            dto.Title.Should().Be(title);
            dto.StartAt.Should().Be(startAt);
            dto.EndAt.Should().Be(endAt);
            dto.TotalSeats.Should().Be(totalSeats);
            dto.Description.Should().Be(description);
        }

        [Fact]
        public void Constructor_WithDefaultDescription_ShouldFallbackToEmptyString()
        {
            // Arrange
            var title = "Конференция по .NET";
            var startAt = _now;
            var endAt = _now.AddHours(2);
            var totalSeats = 50;

            // Act
            var dto = new CreateEventDto(title, startAt, endAt, totalSeats);

            // Assert
            dto.Description.Should().Be(string.Empty);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WhenTitleIsNullOrEmptyOrWhitespace_ShouldThrowArgumentException(string? invalidTitle)
        {
            // Arrange
            var startAt = _now;
            var endAt = _now.AddHours(2);
            var totalSeats = 50;

            // Act
            Action act = () => new CreateEventDto(invalidTitle!, startAt, endAt, totalSeats);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("title");
        }

        [Fact]
        public void Constructor_WhenStartAtIsDefault_ShouldThrowArgumentException()
        {
            // Arrange
            var title = "Конференция по .NET";
            var endAt = _now;
            var totalSeats = 50;

            // Act
            Action act = () => new CreateEventDto(title, default, endAt, totalSeats);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("startAt");
        }

        [Fact]
        public void Constructor_WhenEndAtIsDefault_ShouldThrowArgumentException()
        {
            // Arrange
            var title = "Конференция по .NET";
            var startAt = _now;
            var totalSeats = 50;

            // Act
            Action act = () => new CreateEventDto(title, startAt, default, totalSeats);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("endAt");
        }

        [Fact]
        public void Constructor_WhenEndAtIsLessThanStartAt_ShouldThrowArgumentException()
        {
            // Arrange
            var title = "Конференция по .NET";
            var startAt = _now;
            var invalidEnd = startAt.AddHours(-1); // Конец раньше начала
            var totalSeats = 50;

            // Act
            Action act = () => new CreateEventDto(title, startAt, invalidEnd, totalSeats);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("endAt");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Constructor_WhenTotalSeatsIsLessThanOne_ShouldThrowArgumentException(int invalidSeats)
        {
            // Arrange
            var title = "Конференция по .NET";
            var startAt = _now;
            var endAt = startAt.AddHours(2);

            // Act
            Action act = () => new CreateEventDto(title, startAt, endAt, invalidSeats);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("totalSeats");
        }
    }
}
