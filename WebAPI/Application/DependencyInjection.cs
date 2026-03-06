
using WebAPI.Application.Interfaces;
using WebAPI.Application.Services;

namespace WebAPI.Application
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
