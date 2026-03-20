using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Application.Services;
using EventBookingService.WebAPI.Infrastructure.Persistence;

namespace EventBookingService.WebAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Регистрируем как Singleton, чтобы данные не пропадали между запросами
        services.AddSingleton<IEventRepository, InMemoryEventRepository>();

        services.AddScoped<IEventService, EventService>();

        return services;
    }
}