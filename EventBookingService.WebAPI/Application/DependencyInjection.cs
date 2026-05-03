using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Application.Services;

namespace EventBookingService.WebAPI.Application;

/// <summary>
/// Класс для работы по билдеру
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Метод добавления сервисы
    /// </summary>
    /// <param name="services">Системная коллекция сервисов</param>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}
