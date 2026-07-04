using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Repositories;
using EventForge.Contract.Brokers;

using FluentAssertions;

namespace EventForge.Booking.IntegrationTests;

public class OutboxRepositoryTests : BaseRepositoryTest
{
    private OutboxRepository CreateRepository() => new(Factory);

    [Fact]
    public async Task AddAsync_And_GetPendingAsync_Should_Return_Unprocessed_Messages()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var message = OutboxMessage.Create("BookingConfirmed", TopicNames.BookingConfirmed, "event-key", "payload", DateTime.UtcNow, null);

        await repository.AddAsync(message, CancellationToken.None);
        var result = await repository.GetPendingAsync(10, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(message.Id);
    }

    [Fact]
    public async Task MarkProcessedAsync_Should_Set_ProcessedAt_And_Clear_Error()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var message = OutboxMessage.Restore(Guid.NewGuid(), "BookingConfirmed", TopicNames.BookingConfirmed, "event-key", "payload", DateTime.UtcNow, null, "old error");

        await repository.AddAsync(message, CancellationToken.None);
        await repository.MarkProcessedAsync(message.Id, CancellationToken.None);
        var result = await repository.GetPendingAsync(10, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkFailedAsync_Should_Save_Error_Message()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var message = OutboxMessage.Create("BookingConfirmed", TopicNames.BookingConfirmed, "event-key", "payload", DateTime.UtcNow, null);

        await repository.AddAsync(message, CancellationToken.None);
        await repository.MarkFailedAsync(message.Id, "publish failed", CancellationToken.None);
        await using var context = await CreateContext();
        var entity = await context.OutboxMessages.FindAsync(message.Id);

        entity.Should().NotBeNull();
        entity!.Error.Should().Be("publish failed");
    }
}
