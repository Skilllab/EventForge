using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Application.Services;

namespace EventBookingService.WebAPI.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<IEventService, EventService>();
            return services;
        }
    }
}
