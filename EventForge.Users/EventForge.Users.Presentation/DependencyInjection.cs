using System.Reflection;
using System.Text;

using EventForge.ExceptionMiddleware;
using EventForge.Users.Domain.Exceptions;
using EventForge.Users.Infrastructure.Common;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace EventForge.Users.Presentation;

/// <summary>
/// Класс расширения для регистрации сервисов Presentation-слоя.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует зависимости Presentation-слоя.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <returns>Обновленная коллекция сервисов.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        var jwtOptions = configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();
        var schemeName = jwtOptions?.SchemeName ?? JwtBearerDefaults.AuthenticationScheme;


        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(schemeName, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = "role",
                    NameClaimType = "sub",
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions?.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions?.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions?.Secret ?? string.Empty)),
                    ClockSkew = TimeSpan.Zero,
                };

                options.MapInboundClaims = false;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(StringConstants.CustomJwtPolicy, policy =>
                policy.AddAuthenticationSchemes(schemeName)
                    .RequireAuthenticatedUser());
        });

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
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            });
        });


        services.Configure<ExceptionHandlingOptions>(options =>
        {
            options.ExceptionHandler = exception => exception switch
            {
                ValidationCustomException nfe => (
                    StatusCodes.Status400BadRequest,
                    new ProblemDetails
                    {
                        Type = nfe.EntityName,
                        Instance = nfe.EntityId,
                        Status = StatusCodes.Status400BadRequest,
                        Detail = nfe.Message
                    }
                ),

                // ... другие исключения сервиса
                _ => (StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = exception.Message })
            };
        });



        return services;
    }
}
