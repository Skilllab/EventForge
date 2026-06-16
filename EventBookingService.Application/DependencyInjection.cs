using EventBookingService.Application.Common;
using EventBookingService.Application.Interfaces;
using EventBookingService.Application.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventBookingService.Application;

/// <summary>
/// Класс для работы по билдеру
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Метод добавления сервисы
    /// </summary>
    /// <param name="services">Системная коллекция сервисов</param>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.Configure<BookingOptions>(configuration.GetSection("AuthSettings"));

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();


        return services;
    }
}

