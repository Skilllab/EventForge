using FluentAssertions;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Events.IntegrationTests;

public class DatabaseTests : BaseRepositoryTest
{
    [Fact]
    public async Task Migrations_Should_Apply_Successfully()
    {
        await using var context = await CreateContext();
        await context.Database.EnsureDeletedAsync();

        await context.Database.MigrateAsync();
        var eventsCount = await context.Events.CountAsync();
        var processedCount = await context.ProcessedMessages.CountAsync();

        eventsCount.Should().Be(0);
        processedCount.Should().Be(0);
    }
}
