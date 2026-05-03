using EventBookingService.Data.Context;
using EventBookingService.Data.Interceptors;
using EventBookingService.Data.Repositories;
using EventBookingService.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventBookingService.Data;

/// <summary>
/// Класс для работы по билдеру
/// </summary> 
public static class DependencyInjection
{
    /// <summary>
    /// Метод добавления слоя данных
    /// </summary>
    /// <param name="services">Системная коллекция сервисов</param>
    /// <param name="configuration">Конфигурация проекта</param>
    /// <returns></returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddSingleton<LoggingInterceptor>();
        // Регистрируем фабрику с сервис-провайдером
        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            // Получаем интерцептор из контейнера
            var interceptor = sp.GetRequiredService<LoggingInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            //Не логируем секретные данные
            options.EnableSensitiveDataLogging(false);
            
        });

        // Репозитории
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }
}
