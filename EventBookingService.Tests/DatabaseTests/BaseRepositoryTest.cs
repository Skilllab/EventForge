using EventBookingService.Data.Context;
using EventBookingService.Data.Entities;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace EventBookingService.Tests.DatabaseTests;

public abstract class BaseRepositoryTest : IAsyncLifetime
{
    //Основное подключение. Указываем всё для тестового
    protected readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected IDbContextFactory<AppDbContext> Factory = null!;

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(DbContainer.GetConnectionString())
            .Options;

        // Создаем схему один раз
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        Factory = new TestContextFactory(options);
    }

    public async ValueTask DisposeAsync() => await DbContainer.DisposeAsync();

    /// <summary>
    /// Метод очистки базы для того, чтобы после тестов не оставался мусор
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        await using var context = await Factory.CreateDbContextAsync();

        // Получаем реальные имена таблиц из конфигурации EF
        var bookingTable = context.Model.FindEntityType(typeof(BookingEntity))?.GetTableName();
        var eventTable = context.Model.FindEntityType(typeof(EventEntity))?.GetTableName();

        // EF Core использует схемы. Забираем их
        var bookingSchema = context.Model.FindEntityType(typeof(BookingEntity))?.GetSchema() ?? "public";
        var eventSchema = context.Model.FindEntityType(typeof(EventEntity))?.GetSchema() ?? "public";

        // Собираем полный путь: "schema"."TableName"
        var sql = $"TRUNCATE TABLE \"{bookingSchema}\".\"{bookingTable}\", \"{eventSchema}\".\"{eventTable}\" RESTART IDENTITY CASCADE;";
        await context.Database.ExecuteSqlRawAsync(sql);

    }

    private class TestContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
