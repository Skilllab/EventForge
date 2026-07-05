using FluentAssertions;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Users.IntegrationTests;

public class DatabaseTests : BaseRepositoryTest
{
    [Fact]
    public async Task Migrations_Should_Apply_Successfully()
    {
        await using var context = await CreateContext();
        await context.Database.EnsureDeletedAsync();

        await context.Database.MigrateAsync();
        var count = await context.Users.CountAsync();

        count.Should().Be(0);
    }
}
