using EventForge.Events.Application.Interfaces;
using EventForge.Events.Infrastructure.Common;
using EventForge.Events.Infrastructure.Context;
using EventForge.Events.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Events.Infrastructure
{
    /// <summary>
    /// Регистрация зависимостей слоя Infrastructure.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Регистрирует зависимости слоя Infrastructure.
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContextFactory<EventsDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            });

            services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));

            services.AddScoped<IEventRepository, EventRepository>();

            return services;
        }
    }
}
