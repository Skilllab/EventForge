using EventBookingService.Data.Context;
using EventBookingService.Data.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Npgsql;

using Testcontainers.PostgreSql;

namespace EventBookingService.IntegrationTests;

/// <summary>
/// Базовый класс репозитория
/// </summary>
public abstract class BaseRepositoryTest : IAsyncLifetime
{
    //Основное подключение. Указываем всё для тестового
    protected readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventapi")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();


    protected IDbContextFactory<AppDbContext> Factory = null!;

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddPooledDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(DbContainer.GetConnectionString()));

        var serviceProvider = services.BuildServiceProvider();
        Factory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    }

    public async ValueTask DisposeAsync() => await DbContainer.DisposeAsync();

    protected async Task<AppDbContext> CreateContext()
    {
        var context = await Factory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
        return context;
    }

    public async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var context = await CreateContext();

        // Получаем реальные имена таблиц из конфигурации EF
        var bookingTable = context.Model.FindEntityType(typeof(BookingEntity))?.GetTableName();
        var eventTable = context.Model.FindEntityType(typeof(EventEntity))?.GetTableName();

        // В проекте используется схема
        var bookingSchema = context.Model.FindEntityType(typeof(BookingEntity))?.GetSchema() ?? "public";
        var eventSchema = context.Model.FindEntityType(typeof(EventEntity))?.GetSchema() ?? "public";

        // Собираем полный путь: "schema"."TableName"
        var sql = $"TRUNCATE TABLE \"{bookingSchema}\".\"{bookingTable}\", \"{eventSchema}\".\"{eventTable}\" RESTART IDENTITY CASCADE;";
        await context.Database.ExecuteSqlRawAsync(sql);

    }


}
