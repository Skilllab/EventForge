using EventBookingService.Application.Common;
using EventBookingService.Application.Interfaces;
using EventBookingService.Infrastructure.Common;
using EventBookingService.Infrastructure.Context;
using EventBookingService.Infrastructure.Interceptors;
using EventBookingService.Infrastructure.Repositories;
using EventBookingService.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventBookingService.Infrastructure;

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

        services.AddHostedService<BookingBackgroundService>();

        // Регистрируем фабрику с сервис-провайдером
        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            // Получаем интерцептор из контейнера
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), x =>
                x.MigrationsHistoryTable("__EFMigrationsHistory", "EventBooking"));

            var interceptor = sp.GetRequiredService<LoggingInterceptor>();
            options.AddInterceptors(interceptor);

            //Не логируем секретные данные
            options.EnableSensitiveDataLogging(false);

        });

        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));

        // Репозитории
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Сервисы
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services.AddScoped<IJwtTokenGenerator>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtSettings>>();
            var timeProvider = sp.GetRequiredService<TimeProvider>();
            return new JwtTokenGenerator(options, timeProvider);
        });

        return services;
    }
}

