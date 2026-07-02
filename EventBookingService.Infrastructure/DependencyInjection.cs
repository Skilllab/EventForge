using EventBookingService.Application.Interfaces;
using EventBookingService.Infrastructure.Common;
using EventBookingService.Infrastructure.Context;
using EventBookingService.Infrastructure.Interceptors;
using EventBookingService.Infrastructure.Repositories;
using EventBookingService.Infrastructure.Services;
using EventBookingService.Infrastructure.Services.External;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        //Регистрируем сервис для работы с микросервисом )))
        services.Configure<EventsServiceOptions>(configuration.GetSection(nameof(EventsServiceOptions)));
        services.AddHttpContextAccessor();
        services.AddTransient<AuthorizationHeaderForwardingHandler>();


        // Репозитории
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        
        // Сервисы
        services.AddScoped<ITransactionService, TransactionService>();

        services.AddHttpClient<IEventService, EventsApiClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<EventsServiceOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthorizationHeaderForwardingHandler>();



        return services;
    }
}

