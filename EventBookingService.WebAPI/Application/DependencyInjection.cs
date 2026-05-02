using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Application.Services;
using EventBookingService.WebAPI.Infrastructure.Persistence;

namespace EventBookingService.WebAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}
