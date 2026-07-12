using EventForge.Booking.Application.Common;
using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Application.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Booking.Application;

/// <summary>
/// Регистрация зависимостей слоя Application
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.Configure<BookingOptions>(configuration.GetSection(nameof(BookingOptions)));

        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
