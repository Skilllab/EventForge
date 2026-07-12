using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Mapping;
using EventForge.Booking.Infrastructure.Repositories;
using EventForge.Contract.Brokers;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace EventForge.Booking.IntegrationTests;

public class OutboxRepositoryTests : BaseRepositoryTest
{
    private OutboxRepository CreateRepository() => new(Factory);

    [Fact]
    public async Task GetPendingAsync_Should_Return_Only_Unprocessed_Messages_Ordered_By_CreatedAt()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        var newestPending = OutboxMessage.Create(
            "Бронь подтверждена",
            TopicNames.BookingConfirmed,
            "event-key-2",
            "payload-2",
            DateTime.UtcNow.AddMinutes(-5),
            null);

        var oldestPending = OutboxMessage.Create(
            "Бронь подтверждена",
            TopicNames.BookingConfirmed,
            "event-key-1",
            "payload-1",
            DateTime.UtcNow.AddMinutes(-10),
            null);

        var processed = OutboxMessage.Restore(
            Guid.NewGuid(),
            "Бронь подтверждена",
            TopicNames.BookingConfirmed,
            "event-key-3",
            "payload-3",
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow,
            null);

        await using var context = await CreateContext();
        await context.OutboxMessages.AddRangeAsync(
            oldestPending.ToEntity(),
            newestPending.ToEntity(),
            processed.ToEntity());
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetPendingAsync(10, CancellationToken.None);
        
        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(oldestPending.Id, newestPending.Id);
    }

    [Fact]
    public async Task GetPendingAsync_Should_Respect_Batch_Size()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));

        var first = OutboxMessage.Create("Бронь подтверждена", TopicNames.BookingConfirmed, "key-1", "payload-1", fakeTimeProvider.GetUtcNow().UtcDateTime.AddMinutes(-3), null);
        var second = OutboxMessage.Create("Бронь подтверждена", TopicNames.BookingConfirmed, "key-2", "payload-2", fakeTimeProvider.GetUtcNow().UtcDateTime.AddMinutes(-2), null);
        var third = OutboxMessage.Create("Бронь подтверждена", TopicNames.BookingConfirmed, "key-3", "payload-3", fakeTimeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1), null);

        await using var context = await CreateContext();
        await context.OutboxMessages.AddRangeAsync(first.ToEntity(), second.ToEntity(), third.ToEntity());
        await context.SaveChangesAsync();

        // Act  
        var result = await repository.GetPendingAsync(2, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(first.Id, second.Id);
    }

    [Fact]
    public async Task MarkProcessedAsync_Should_Set_ProcessedAt_And_Clear_Error()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var message = OutboxMessage.Restore(
            Guid.NewGuid(),
            "Бронь подтверждена",
            TopicNames.BookingConfirmed,
            "event-key",
            "payload",
            fakeTimeProvider.GetUtcNow().UtcDateTime,
            null,
            "старая ошибка");

        await using var arrangeContext = await CreateContext();
        await arrangeContext.OutboxMessages.AddAsync(message.ToEntity());
        await arrangeContext.SaveChangesAsync();

        await repository.MarkProcessedAsync(message.Id, CancellationToken.None);

        // Act
        await using var assertContext = await CreateContext();
        var entity = await assertContext.OutboxMessages.FindAsync(message.Id);

        // Assert
        entity.Should().NotBeNull();
        entity!.ProcessedAt.Should().NotBeNull();
        entity.Error.Should().BeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_Should_Save_Error_Message()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var message = OutboxMessage.Create(
            "BookingConfirmed",
            TopicNames.BookingConfirmed,
            "event-key",
            "payload",
            fakeTimeProvider.GetUtcNow().UtcDateTime,
            null);

        await using var arrangeContext = await CreateContext();
        await arrangeContext.OutboxMessages.AddAsync(message.ToEntity());
        await arrangeContext.SaveChangesAsync();

        // Act
        await repository.MarkFailedAsync(message.Id, "публикация провалена", CancellationToken.None);

        await using var assertContext = await CreateContext();
        var entity = await assertContext.OutboxMessages.FindAsync(message.Id);

        // Assert
        entity.Should().NotBeNull();
        entity!.Error.Should().Be("публикация провалена");
        entity.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task MarkProcessedAsync_Should_Do_Nothing_When_Message_Not_Found()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        // Act
        var act = async () => await repository.MarkProcessedAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MarkFailedAsync_Should_Do_Nothing_When_Message_Not_Found()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        // Act
        var act = async () => await repository.MarkFailedAsync(Guid.NewGuid(), "публикация провалена", CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
