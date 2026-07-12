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

        return services;
    }
}
