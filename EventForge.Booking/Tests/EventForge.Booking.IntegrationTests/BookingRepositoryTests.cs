using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Repositories;
using EventForge.Contract.Brokers;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Booking.IntegrationTests;

public class BookingRepositoryTests : BaseRepositoryTest
{
    private BookingRepository CreateRepository() => new(Factory);

    [Fact]
    public async Task AddAsync_Should_Save_Booking_And_GetByIdAsync_Should_Return_It()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        await repository.AddAsync(booking, CancellationToken.None);
        var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetUserActiveBookingsCountAsync_Should_Count_Only_Pending_And_Confirmed()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var userId = Guid.NewGuid();
        var pending = BookingModel.Create(Guid.NewGuid(), userId, DateTime.UtcNow);
        var confirmed = BookingModel.Create(Guid.NewGuid(), userId, DateTime.UtcNow);
        confirmed.Confirm(DateTime.UtcNow);
        var cancelled = BookingModel.Create(Guid.NewGuid(), userId, DateTime.UtcNow);
        cancelled.Cancel(DateTime.UtcNow);

        await repository.AddAsync(pending, CancellationToken.None);
        await repository.AddAsync(confirmed, CancellationToken.None);
        await repository.AddAsync(cancelled, CancellationToken.None);

        var result = await repository.GetUserActiveBookingsCountAsync(userId, CancellationToken.None);

        result.Should().Be(2);
    }

    [Fact]
    public async Task ConfirmAndAddOutboxAsync_Should_Update_Status_And_Create_Outbox_Record()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        await repository.AddAsync(booking, CancellationToken.None);
        var processedAt = DateTime.UtcNow;
        var outbox = OutboxMessage.Create(nameof(BookingConfirmed), TopicNames.BookingConfirmed, booking.EventId.ToString(), "payload", processedAt, null);

        var updated = await repository.ConfirmAndAddOutboxAsync(booking.Id, processedAt, outbox, CancellationToken.None);
        var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);
        await using var context = await CreateContext();
        var outboxCount = await context.OutboxMessages.CountAsync();

        updated.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Status.Should().Be(BookingStatus.Confirmed);
        outboxCount.Should().Be(1);
    }

    [Fact]
    public async Task CancelAndAddOutboxAsync_Should_Return_False_When_User_Does_Not_Own_Booking()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        await repository.AddAsync(booking, CancellationToken.None);
        var outbox = OutboxMessage.Create(nameof(BookingCancelled), TopicNames.BookingCancelled, booking.EventId.ToString(), "payload", DateTime.UtcNow, null);

        var updated = await repository.CancelAndAddOutboxAsync(booking.Id, Guid.NewGuid(), DateTime.UtcNow, outbox, CancellationToken.None);

        updated.Should().BeFalse();
    }
}
