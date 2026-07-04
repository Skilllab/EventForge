using EventForge.Events.Application.Interfaces;
using EventForge.Events.Infrastructure.Common;
using EventForge.Events.Infrastructure.Context;
using EventForge.Events.Infrastructure.Repositories;
using EventForge.Events.Infrastructure.Services;
using EventForge.LoggingDBInterceptor;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Events.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует зависимости слоя Infrastructure.
    /// </summary>
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

        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingConfirmedConsumer>();
        services.AddHostedService<BookingCancelledConsumer>();
        services.AddHostedService<BookingRejectedConsumer>();

        return services;
    }
}
