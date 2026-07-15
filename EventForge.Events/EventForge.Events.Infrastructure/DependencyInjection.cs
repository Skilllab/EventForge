using EventForge.Events.Application.Interfaces;
using EventForge.Events.Infrastructure.Context;
using EventForge.Events.Infrastructure.Entities;
using EventForge.Events.Infrastructure.Repositories;
using EventForge.Events.Infrastructure.Services;
using EventForge.LoggingDBInterceptor;
using EventForge.Settings.JWT;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

namespace EventForge.Events.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<LoggingInterceptor>();

        services.AddDbContextFactory<EventsDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));
        services.Configure<KafkaOptions>(configuration.GetSection(nameof(KafkaOptions)));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        services.AddHostedService<BookingRequestedConsumer>();
        services.AddHostedService<BookingCancelledConsumer>();

        services.AddHostedService<OutboxPublisherBackgroundService>();


        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration.GetConnectionString("Redis"));
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 3;
            options.ConnectTimeout = 5000; // Тайм-аут подключения, мс
            options.SyncTimeout = 3000;      // Тайм-аут синхронных операций, мс

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}
