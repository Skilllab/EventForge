using EventForge.Users.Application.Interfaces;
using EventForge.Users.Application.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Users.Application;

/// <summary>
/// Класс расширения для регистрации сервисов Application-слоя
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
