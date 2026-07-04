using FluentAssertions;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Booking.IntegrationTests;

public class DatabaseTests : BaseRepositoryTest
{
    [Fact]
    public async Task Migrations_Should_Apply_Successfully()
    {
        await using var context = await CreateContext();
        await context.Database.EnsureDeletedAsync();

        await context.Database.MigrateAsync();
        var bookingsCount = await context.Bookings.CountAsync();
        var outboxCount = await context.OutboxMessages.CountAsync();

        bookingsCount.Should().Be(0);
        outboxCount.Should().Be(0);
    }
}
