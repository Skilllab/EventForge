using EventForge.Events.Domain.Entities;
using EventForge.Events.Infrastructure.Repositories;

using FluentAssertions;

namespace EventForge.Events.IntegrationTests;

public class EventRepositoryTests : BaseRepositoryTest
{
    private EventRepository CreateRepository() => new(Factory);

    [Fact]
    public async Task AddAsync_Should_Save_Event_With_Initial_AvailableSeats()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var startAt = new DateTime(2025, 8, 20, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Events integration", startAt, startAt.AddHours(3), 40, "Integration test");

        await repository.AddAsync(evt, CancellationToken.None);
        var result = await repository.GetByIdAsync(evt.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Events integration");
        result.AvailableSeats.Should().Be(40);
    }

    [Fact]
    public async Task GetPagedAsync_Should_Filter_By_Title_And_Date_Range()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var baseDate = new DateTime(2025, 9, 1, 9, 0, 0, DateTimeKind.Utc);
        var target = Event.Create("Dotnet Meetup", baseDate, baseDate.AddHours(2), 20);
        var other = Event.Create("Frontend Meetup", baseDate.AddDays(5), baseDate.AddDays(5).AddHours(2), 20);

        await repository.AddAsync(target, CancellationToken.None);
        await repository.AddAsync(other, CancellationToken.None);

        var result = await repository.GetPagedAsync("dotnet", baseDate.AddHours(-1), baseDate.AddDays(1), 1, 10, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task TryReserveSeatAsync_And_ReleaseSeatAsync_Should_Update_AvailableSeats()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var startAt = new DateTime(2025, 8, 21, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Seats event", startAt, startAt.AddHours(1), 3);
        await repository.AddAsync(evt, CancellationToken.None);

        var reserved = await repository.TryReserveSeatAsync(evt.Id, 2, CancellationToken.None);
        await repository.ReleaseSeatAsync(evt.Id, 1, CancellationToken.None);
        var result = await repository.GetByIdAsync(evt.Id, CancellationToken.None);

        reserved.Should().BeTrue();
        result.Should().NotBeNull();
        result!.AvailableSeats.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_Should_Return_True_When_Event_Exists()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var startAt = new DateTime(2025, 8, 22, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Delete event", startAt, startAt.AddHours(1), 5);
        await repository.AddAsync(evt, CancellationToken.None);

        var deleted = await repository.DeleteAsync(evt.Id, CancellationToken.None);
        var result = await repository.GetByIdAsync(evt.Id, CancellationToken.None);

        deleted.Should().BeTrue();
        result.Should().BeNull();
    }
}
