using System.Reflection;

using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace EventForge.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSharedSwagger(this IServiceCollection services, string apiTitle)
    {
        var apiVersioningBuilder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0); // Версия по умолчанию
            options.AssumeDefaultVersionWhenUnspecified = true; // Использовать дефолтную версию, если она не указана
            options.ReportApiVersions = true; // Отдавать доступные версии в заголовках ответа
            options.ApiVersionReader = new UrlSegmentApiVersionReader(); // Читать версию из URL
        });

        // 2. Интеграция версионирования с ApiExplorer
        apiVersioningBuilder.AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV"; // Название группы в формате 'v1', 'v2'
            options.SubstituteApiVersionInUrl = true; // Подставлять версию в роуты эндпоинтов
        });

        services.AddOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>()
            .Configure<IServiceProvider>((options, sp) =>
            {
                var provider = sp.GetService<IApiVersionDescriptionProvider>();

                if (provider != null)
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerDoc(description.GroupName, new OpenApiInfo
                        {
                            Title = $"{apiTitle} {description.ApiVersion}",
                            Version = description.GroupName
                        });
                    }
                }
                else
                {
                    options.SwaggerDoc("v1", new OpenApiInfo { Title = apiTitle, Version = "v1" });
                }

                var xmlFile = $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                options.EnableAnnotations();

                var securityScheme = new OpenApiSecurityScheme
                {
                    Description = "Введите JWT токен в формате: Bearer {ваш_токен}",
                    Name = "Authorization",
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    Type = SecuritySchemeType.Http,
                    In = ParameterLocation.Header,
                };

                options.AddSecurityDefinition("Bearer", securityScheme);

                // ИСПРАВЛЕНИЕ: Новый синтаксис с делегатом документа для .NET 10 / Swashbuckle v10
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

        services.AddSwaggerGen();

        return services;
    }

    // 2. Метод для настройки Middleware (app.UseSharedSwaggerUI)
    public static IApplicationBuilder UseSharedSwaggerUI(this IApplicationBuilder app, string serviceName)
    {
        // Раздаём embedded-ресурсы (CSS) через MapGet
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value;
            string? resourceName = path switch
            {
                "/custom-swagger.css" => "EventForge.Swagger.wwwroot.custom-swagger.css",
                "/favicon.ico" => "EventForge.Swagger.wwwroot.favicon.ico",
                "/logo.png" => "EventForge.Swagger.wwwroot.logo.png",
                _ => null
            };

            if (resourceName != null)
            {
                var assembly = typeof(SwaggerExtensions).Assembly;
                await using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    context.Response.ContentType = path switch
                    {
                        "/custom-swagger.css" => "text/css",
                        "/favicon.ico" => "image/x-icon",
                        "/logo.png" => "image/png",
                        _ => "application/octet-stream"
                    };
                    context.Response.Headers.CacheControl = "public, max-age=3600";
                    await stream.CopyToAsync(context.Response.Body);
                    return;
                }
            }
            await next();
        });


        app.UseSwaggerUI(options =>
        {
            // Динамически регистрируем конечные точки для всех обнаруженных версий
            var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"EventForge {serviceName} {description.GroupName.ToUpperInvariant()}"
                );
            }

            options.DisplayRequestDuration();
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.DocumentTitle = $"Сервис {serviceName}";
            // Внедряем JS-скрипт, который удаляет старые иконки и принудительно ставит вашу
            options.HeadContent = @"
        <script>
            document.addEventListener('DOMContentLoaded', function() {
                // Удаляем все стандартные теги иконок Swagger
                const existingIcons = document.querySelectorAll(""link[rel*='icon']"");
                existingIcons.forEach(icon => icon.remove());

                // Создаем и добавляем вашу новую иконку
                const link = document.createElement('link');
                link.type = 'image/x-icon';
                link.rel = 'icon';
                link.href = '/favicon.ico';
                document.head.appendChild(link);
            });
        </script>";

            // Скрипт для динамического добавления текста в шапку
            options.HeadContent += @"
        <script>
            document.addEventListener('DOMContentLoaded', function() {
                // Ждем появления элемента ссылки в шапке
                const checkTopbar = setInterval(() => {
                    const topbarLink = document.querySelector('.swagger-ui .topbar .link');
                    
                    if (topbarLink) {
                        clearInterval(checkTopbar); // Останавливаем проверку
                        
                        // Создаем контейнер для нашего текста
                        const brandText = document.createElement('span');
                        brandText.className = 'custom-topbar-text';
                        brandText.innerText = 'EventForge - лучшая микросервисная платформа';
                        
                        // Добавляем текст внутрь ссылки в шапке
                        topbarLink.appendChild(brandText);
                    }
                }, 50); // Проверяем каждые 50мс
            });
        </script>";

            // Скрипт для изменения текста "Select a definition"
            options.HeadContent += @"
        <script>
            document.addEventListener('DOMContentLoaded', function() {
                // Создаем наблюдатель за изменениями на странице
                const observer = new MutationObserver((mutations, obs) => {
                    // Ищем лейбл выпадающего списка
                    const label = document.querySelector('.swagger-ui .topbar .download-url-wrapper label span');
                    
                    if (label) {
                        // Заменяем текст на ваш собственный
                        label.textContent = 'Выберите версию API:'; // ВПИШИТЕ СЮДА ВАШ ТЕКСТ
                        obs.disconnect(); // Отключаем наблюдатель, когда текст изменен
                    }
                });

                // Начинаем следить за всем документом
                observer.observe(document.body, {
                    childList: true,
                    subtree: true
                });
            });
        </script>";



            // Подключаем кастомные стили из папки wwwroot
            options.InjectStylesheet("/custom-swagger.css");
        });

        return app;
    }
}
