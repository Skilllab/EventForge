using EventForge.Events.Application.Interfaces;
using EventForge.Events.Application.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Events.Application;

/// <summary>
/// Регистрация зависимостей слоя Application
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует зависимости слоя Application
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IEventService, EventService>();

        return services;
    }
}
