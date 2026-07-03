
using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Infrastructure.Common;
using EventForge.Booking.Infrastructure.Context;
using EventForge.Booking.Infrastructure.Repositories;
using EventForge.Booking.Infrastructure.Services;
using EventForge.Booking.Infrastructure.Services.External;
using EventForge.LoggingDBInterceptor;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace EventForge.Booking.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<LoggingInterceptor>();


        services.AddDbContextFactory<BookingDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));

        services.AddHostedService<BookingBackgroundService>();

        services.AddScoped<IBookingRepository, BookingRepository>();
        //Регистрируем сервис для работы с микросервисом )))
        services.Configure<EventsServiceOptions>(configuration.GetSection(nameof(EventsServiceOptions)));
        services.AddHttpContextAccessor();
        services.AddTransient<AuthorizationHeaderForwardingHandler>();

        services.AddHttpClient<IEventsGateway, EventsApiClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<EventsServiceOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthorizationHeaderForwardingHandler>();

        return services;
    }
}
