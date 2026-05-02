using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Data;
using EventBookingService.WebAPI.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

namespace EventBookingService.WebAPI.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // База данных
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Репозитории
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();

            return services;
        }
    }
}
