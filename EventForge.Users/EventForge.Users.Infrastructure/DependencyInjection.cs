using EventForge.LoggingDBInterceptor;
using EventForge.Settings.JWT;
using EventForge.Users.Application.Interfaces;
using EventForge.Users.Infrastructure.Context;
using EventForge.Users.Infrastructure.Repositories;
using EventForge.Users.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventForge.Users.Infrastructure;

/// <summary>
/// Класс расширения для регистрации сервисов Infrastructure-слоя
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<LoggingInterceptor>();

        services.AddDbContextFactory<UsersDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IJwtTokenGenerator>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<JwtSettings>>();
            var tp = sp.GetRequiredService<TimeProvider>();
            return new JwtTokenGenerator(opts, tp);
        });

        return services;
    }
}
