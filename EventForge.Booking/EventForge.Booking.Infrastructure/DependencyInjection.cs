
using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Infrastructure.Common;
using EventForge.Booking.Infrastructure.Context;
using EventForge.Booking.Infrastructure.Repositories;
using EventForge.Booking.Infrastructure.Services;
using EventForge.LoggingDBInterceptor;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


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
        services.Configure<KafkaOptions>(configuration.GetSection(nameof(KafkaOptions)));


        // Фоновая обработка pending бронирований.
        services.AddHostedService<BookingBackgroundService>();

        // Фоновая публикация сообщений из outbox.
        services.AddHostedService<OutboxPublisherBackgroundService>();

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        services.AddSingleton<IBookingConfirmedPublisher, KafkaBookingConfirmedPublisher>();
       
        return services;
    }
}
