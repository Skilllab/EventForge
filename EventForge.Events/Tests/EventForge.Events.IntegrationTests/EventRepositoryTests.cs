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
        var evt = Event.Create("Событие интеграции", startAt, startAt.AddHours(3), 40, "Тест интеграции");

        await repository.AddAsync(evt, CancellationToken.None);
        var result = await repository.GetByIdAsync(evt.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Событие интеграции");
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
    public async Task SaveEventAndOutboxAsync_Should_Persist_Event_And_Outbox_In_Single_Transaction()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var startAt = new DateTime(2025, 8, 21, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Событие с местами", startAt, startAt.AddHours(1), 5);

        await repository.AddAsync(evt, CancellationToken.None);

        // Резервируем через домен
        evt.TryReserveSeats(2);
        var outbox = OutboxMessage.Create("Тестовое сообщение", "topic", "key", "{}", DateTime.UtcNow, null);

        await repository.SaveEventAndOutboxAsync(evt, outbox, CancellationToken.None);

        // Проверяем, что места сохранились
        var result = await repository.GetByIdAsync(evt.Id, CancellationToken.None);
        result.Should().NotBeNull();
        result!.AvailableSeats.Should().Be(3);
    }

    [Fact]
    public async Task DeleteAsync_Should_Return_True_When_Event_Exists()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var startAt = new DateTime(2025, 8, 22, 10, 0, 0, DateTimeKind.Utc);
        var evt = Event.Create("Удаляемое событие", startAt, startAt.AddHours(1), 5);
        await repository.AddAsync(evt, CancellationToken.None);

        var deleted = await repository.DeleteAsync(evt.Id, CancellationToken.None);
        var result = await repository.GetByIdAsync(evt.Id, CancellationToken.None);

        deleted.Should().BeTrue();
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Should_Return_False_When_Event_Does_Not_Exist()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        var deleted = await repository.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        deleted.Should().BeFalse();
    }
}
