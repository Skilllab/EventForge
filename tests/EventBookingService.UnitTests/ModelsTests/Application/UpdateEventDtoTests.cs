using EventBookingService.Application.DTO;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace EventBookingService.UnitTests.ModelsTests.Application
{
    public class UpdateEventDtoTests
    {

        private readonly DateTime _now;

        public UpdateEventDtoTests()
        {
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            _now = fixedUtcNow.UtcDateTime;
        }


        [Fact]
        public void Create_WithValidParameters_ShouldCreateObjectSuccessfully()
        {
            // Arrange
            var title = "Обновленное название";
            var description = "Новое описание";
            var startAt = _now;
            var endAt = _now.AddHours(3);

            // Act
            var dto = UpdateEventDto.Create(title, startAt, endAt, description);

            // Assert
            dto.Should().NotBeNull();
            dto.Title.Should().Be(title);
            dto.Description.Should().Be(description);
            dto.StartAt.Should().Be(startAt);
            dto.EndAt.Should().Be(endAt);
        }

        [Fact]
        public void Create_WithNoParameters_ShouldCreateObjectWithNulls()
        {
            // Act
            var dto = UpdateEventDto.Create();

            // Assert
            dto.Should().NotBeNull();
            dto.Title.Should().BeNull();
            dto.Description.Should().BeNull();
            dto.StartAt.Should().BeNull();
            dto.EndAt.Should().BeNull();
        }

        [Fact]
        public void Create_WhenEndAtIsLessThanStartAt_ShouldThrowArgumentException()
        {
            // Arrange
            var startAt = _now.AddDays(1);
            var invalidEndAt = _now.AddHours(-2); // Конец раньше начала на 2 часа

            // Act
            Action act = () => UpdateEventDto.Create(startAt: startAt, endAt: invalidEndAt);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("endAt");
        }

        [Fact]
        public void Create_WhenEndAtEqualsStartAt_ShouldCreateSuccessfully()
        {
            // Arrange
            var sameTime = _now.AddDays(1);

            // Act
            var dto = UpdateEventDto.Create(startAt: sameTime, endAt: sameTime);

            // Assert
            dto.Should().NotBeNull();
            dto.StartAt.Should().Be(sameTime);
            dto.EndAt.Should().Be(sameTime);
        }

        [Fact]
        public void Create_WhenOnlyStartAtIsProvided_ShouldCreateSuccessfully()
        {
            // Arrange
            var startAt = _now.AddDays(1);

            // Act
            var dto = UpdateEventDto.Create(startAt: startAt);

            // Assert
            dto.Should().NotBeNull();
            dto.StartAt.Should().Be(startAt);
            dto.EndAt.Should().BeNull();
        }

        [Fact]
        public void Create_WhenOnlyEndAtIsProvided_ShouldCreateSuccessfully()
        {
            // Arrange
            var endAt = _now.AddDays(1);

            // Act
            var dto = UpdateEventDto.Create(endAt: endAt);

            // Assert
            dto.Should().NotBeNull();
            dto.StartAt.Should().BeNull();
            dto.EndAt.Should().Be(endAt);
        }
    }
}
