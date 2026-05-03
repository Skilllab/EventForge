using EventBookingService.Data.Repositories;
using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Interfaces;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace EventBookingService.Tests.DatabaseTests;

public class EventRepositoryTests : BaseRepositoryTest
{
    private IEventRepository CreateRepo() => new EventRepository(Factory);

    [Fact]
    public async Task AddAsync_ShouldSaveEvent_WithCorrectInitialState()
    {
        // Arrange
        await CleanupDatabaseAsync();
        var repo = CreateRepo();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var title = "Пенная вечеринка у Киркорова";
        var description = "Будут все";
        var totalSeats = 100;

        var @event = Event.Create(title, now.AddDays(1), now.AddDays(2), totalSeats, description);

        // Act
        await repo.AddAsync(@event, CancellationToken.None);

        // Assert
        var result = await repo.GetByIdAsync(@event.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(@event.Id);
        result.TotalSeats.Should().Be(totalSeats);
        result.AvailableSeats.Should().Be(totalSeats);
        result.Title.Should().Be(title);
        result.Description.Should().Be(description);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistAvailableSeats_AfterReservation()
    {
        // Arrange
        await CleanupDatabaseAsync();
        var repo = CreateRepo();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var title = "Пенная вечеринка у Киркорова";
        var description = "Будут все";
        var totalSeats = 10;

        var @event = Event.Create(title, now, now.AddDays(1), totalSeats, description);
        await repo.AddAsync(@event, CancellationToken.None);

        // Act
        @event.TryReserveSeats(3);
        await repo.UpdateAsync(@event, CancellationToken.None);

        // Assert
        var updated = await repo.GetByIdAsync(@event.Id, CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.AvailableSeats.Should().Be(7);
        updated.Title.Should().Be(title);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldApplyAllFilters()
    {
        // Arrange
        await CleanupDatabaseAsync();
        var repo = CreateRepo();

        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;
        var title = "Пенная вечеринка у Киркорова";
        var title2 = "Пенная вечеринка у Ждеймса Хетфилда";
        var description = "Будут все";
        var totalSeats = 10;

        var e1 = Event.Create(title, now.AddDays(1), now.AddDays(1), totalSeats, description);
        var e2 = Event.Create(title2, now.AddDays(5), now.AddDays(5), totalSeats, description);

        await repo.AddAsync(e1, CancellationToken.None);
        await repo.AddAsync(e2, CancellationToken.None);

        // Act
        var result = await repo.GetPagedAsync(title, now, now.AddDays(2), 1, 10, CancellationToken.None);

        // Assert
        result.Items.Should().ContainSingle()
            .Which.Title.Should().Be(title);
    }

    [Fact]
    public async Task GetPagedAsync_Pagination_ShouldReturnCorrectSlice()
    {
        // Arrange
        await CleanupDatabaseAsync();
        var repo = CreateRepo();

        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime;

        var description = "Будут все";
        var totalSeats = 10;

        for (var i = 1; i <= 3; i++)
        {

            var title = $"Пенная вечеринка у Киркорова {i}";
            var e = Event.Create(title, now, now.AddDays(i), totalSeats, description);
            await repo.AddAsync(e, CancellationToken.None);
        }

        // Act: 2-я страница, размер 1. Ожидаем Event 2
        var result = await repo.GetPagedAsync(null, null, null, 2, 1, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Пенная вечеринка у Киркорова 2");
    }

    [Fact]
    public async Task GetPagedAsync_EndAtWithZeroTime_ShouldIncludeWholeDay()
    {
        // Arrange
        await CleanupDatabaseAsync();
        var repo = CreateRepo();

        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime.Date;
        var title = "Пенная вечеринка у Киркорова";
        var description = "Будут все";
        var totalSeats = 10;

        var @event = Event.Create(title, now, now.AddHours(15), totalSeats, description);
        await repo.AddAsync(@event, CancellationToken.None);

        // Act
        var result = await repo.GetPagedAsync(null, null, now, 1, 10, CancellationToken.None);

        // Assert
        result.Items.Should().NotBeEmpty()
            .And.Contain(e => e.Title == title);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenEventExists()
    {
        // Arrange
        await CleanupDatabaseAsync();
        var repo = CreateRepo();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var now = fixedUtcNow.UtcDateTime.Date;
        var title = "Пенная вечеринка у Киркорова";
        var description = "Будут все";
        var totalSeats = 10;

        var @event = Event.Create(title, now, now.AddDays(1).AddHours(1), totalSeats, description);
        await repo.AddAsync(@event, CancellationToken.None);

        // Act
        var isDeleted = await repo.DeleteAsync(@event.Id, CancellationToken.None);

        // Assert
        isDeleted.Should().BeTrue();
        var result = await repo.GetByIdAsync(@event.Id, CancellationToken.None);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetPagedAsync_ShouldThrow_OnInvalidParameters(int page, int pageSize)
    {
        // Arrange
        var repo = CreateRepo();

        // Act
        Func<Task> act = () => repo.GetPagedAsync(null, null, null, page, pageSize, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
