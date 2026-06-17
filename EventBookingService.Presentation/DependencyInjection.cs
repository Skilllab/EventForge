using System.Reflection;
using System.Text;

using EventBookingService.Infrastructure.Common;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace EventBookingService.Presentation;

/// <summary>
/// Класс для работы по билдеру
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Метод добавления контроллеров, эндпоинтов и сваггера
    /// </summary>
    /// <param name="services">Системная коллекция сервисов</param>
    /// <param name="configuration">Конфигурация приложения</param>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        var jwtOptions = configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions?.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions?.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions?.Secret ?? string.Empty)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();


        services.AddSwaggerGen(options =>
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Введите JWT токен в формате: Bearer {ваш_токен}",
                Name = "Authorization",
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Type = SecuritySchemeType.Http,
                In = ParameterLocation.Header,
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

        });

        return services;
    }
}
