using System.ComponentModel.DataAnnotations;

using EventBookingService.Application.DTO;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace EventBookingService.UnitTests.ModelsTests.Application;

public class EventsFilterDtoTests
{

    private readonly DateTime _now;

    public EventsFilterDtoTests()
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
        var title = "Митап";
        var from = _now;
        var to = _now.AddDays(2);
        var page = 2;
        var pageSize = 20;

        // Act
        var filter = new EventsFilterDto(title, from, to, page, pageSize);

        // Assert
        filter.Should().NotBeNull();
        filter.Title.Should().Be(title);
        filter.From.Should().Be(from);
        filter.To.Should().Be(to);
        filter.Page.Should().Be(page);
        filter.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public void Constructor_WithDefaultParameters_ShouldApplyDefaults()
    {
        // Act
        var filter = new EventsFilterDto();

        // Assert
        filter.Title.Should().BeNull();
        filter.From.Should().BeNull();
        filter.To.Should().BeNull();
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(10);
    }

    [Fact]
    public void Constructor_WhenTitleLengthIsExactly100_ShouldCreateSuccessfully()
    {
        // Arrange
        var validTitle = new string('A', 100);

        // Act
        var filter = new EventsFilterDto(title: validTitle);

        // Assert
        filter.Title.Should().HaveLength(100);
    }

    [Fact]
    public void Constructor_WhenTitleLengthExceeds100_ShouldThrowValidationException()
    {
        // Arrange
        var invalidTitle = new string('A', 101);

        // Act
        Action act = () => new EventsFilterDto(title: invalidTitle);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WhenPageIsLessThanOne_ShouldThrowValidationException(int invalidPage)
    {
        // Act
        Action act = () => new EventsFilterDto(page: invalidPage);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void Constructor_WhenPageSizeIsOutOfRange_ShouldThrowValidationException(int invalidPageSize)
    {
        // Act
        Action act = () => new EventsFilterDto(pageSize: invalidPageSize);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Constructor_WhenPageSizeIsAtBoundaries_ShouldCreateSuccessfully(int boundaryPageSize)
    {
        // Act
        var filter = new EventsFilterDto(pageSize: boundaryPageSize);

        // Assert
        filter.PageSize.Should().Be(boundaryPageSize);
    }

    [Fact]
    public void Constructor_WhenToIsLessThanFrom_ShouldThrowValidationException()
    {
        // Arrange
        var fromDate = _now.AddDays(5);
        var invalidToDate = _now.AddDays(4); // Конец раньше начала

        // Act
        Action act = () => new EventsFilterDto(from: fromDate, to: invalidToDate);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Constructor_WhenToEqualsFrom_ShouldCreateSuccessfully()
    {
        // Arrange
        var sameDate = _now;

        // Act
        var filter = new EventsFilterDto(from: sameDate, to: sameDate);

        // Assert
        filter.From.Should().Be(sameDate);
        filter.To.Should().Be(sameDate);
    }

    [Fact]
    public void Constructor_WhenOnlyFromIsProvided_ShouldCreateSuccessfully()
    {
        // Arrange
        var fromDate = _now;

        // Act
        var filter = new EventsFilterDto(from: fromDate, to: null);

        // Assert
        filter.From.Should().Be(fromDate);
        filter.To.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenOnlyToIsProvided_ShouldCreateSuccessfully()
    {
        // Arrange
        var toDate = _now;

        // Act
        var filter = new EventsFilterDto(from: null, to: toDate);

        // Assert
        filter.From.Should().BeNull();
        filter.To.Should().Be(toDate);
    }
}
