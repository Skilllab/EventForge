using System.ComponentModel.DataAnnotations;

using EventBookingService.Application.DTO;

using FluentAssertions;

namespace EventBookingService.UnitTests;

public class EventsFilterTests
{
    [Fact]
    public void EventsFilter_ShouldBeValid_WithDefaultValues()
    {
        // Arrange
        var filter = new EventsFilter();

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EventsFilter_ShouldHaveError_WhenPageIsLessThenOne(int invalidPage)
    {
        // Arrange
        var filter = new EventsFilter { page = invalidPage };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(x => x.MemberNames.Contains("page"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void EventsFilter_ShouldHaveError_WhenPageSizeIsOutOfRange(int invalidSize)
    {
        // Arrange
        var filter = new EventsFilter { pageSize = invalidSize };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(x => x.MemberNames.Contains("pageSize"));
        results.Should().Contain(x => x.ErrorMessage!.Contains("от 1 до 100"));
    }

    [Fact]
    public void EventsFilter_ShouldHaveError_WhenTitleIsTooLong()
    {
        // Arrange
        var filter = new EventsFilter { title = new string('a', 101) };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(x => x.MemberNames.Contains("title"));
    }

    [Fact]
    public void EventsFilter_ShouldHaveError_WhenToDateIsBeforeFromDate()
    {
        // Arrange
        var filter = new EventsFilter
        {
            from = DateTime.Now.AddHours(5),
            to = DateTime.Now.AddHours(2)
        };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(x => x.MemberNames.Contains("to"));
    }

    [Fact]
    public void EventsFilter_ShouldBeValid_WhenOnlyOneDateIsProvided()
    {
        // Arrange
        var filter = new EventsFilter { from = DateTime.Now };

        // Act
        var results = ValidateModel(filter);

        // Assert
        results.Should().BeEmpty();
    }

    // Вспомогательный метод для запуска валидации
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var ctx = new ValidationContext(model, null, null);

        var validationResults = new List<ValidationResult>();

        Validator.TryValidateObject(model, ctx, validationResults, true);

        return validationResults;
    }

}
