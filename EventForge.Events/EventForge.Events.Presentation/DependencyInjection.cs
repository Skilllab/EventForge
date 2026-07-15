using System.Reflection;
using System.Text;

using EventForge.Events.Domain.Exceptions;
using EventForge.ExceptionMiddleware;
using EventForge.Settings.JWT;
using EventForge.Shared.Constants;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace EventForge.Events.Presentation;

/// <summary>
/// Регистрация зависимостей слоя Presentation
/// </summary>
public static class DependencyInjection
{
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
            //Используем кастомную схему аутентификации JWT Bearer
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
                    ClockSkew = TimeSpan.Zero
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
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

        });


        services.Configure<ExceptionHandlingOptions>(options =>
        {
            options.ExceptionHandler = exception => exception switch
            {
                NotFoundException nfe => (
                    StatusCodes.Status404NotFound,
                    new ProblemDetails
                    {
                        Type = nfe.EntityName,
                        Instance = nfe.EntityId,
                        Status = StatusCodes.Status404NotFound,
                        Detail = nfe.Message
                    }
                ),

                // ... другие исключения сервиса
                _ => (StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = exception.Message })
            };
        });


        // Добавляем CORS-политику
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()    // Разрешить запросы с любого URL (включая все ваши порты Swagger)
                    .AllowAnyHeader()    // Разрешить любые заголовки (включая Authorization с JWT)
                    .AllowAnyMethod();   // Разрешить любые методы (GET, POST и т.д.)
            });
        });

        return services;
    }
}
