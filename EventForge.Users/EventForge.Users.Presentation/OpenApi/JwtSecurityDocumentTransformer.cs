using System.Reflection;
using System.Xml.XPath;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EventForge.Users.Presentation.OpenApi
{
    public class JwtSecurityDocumentTransformer : IOpenApiDocumentTransformer
    {
        private readonly string _schemeName;

        // Внедряем настройки через конструктор
        public JwtSecurityDocumentTransformer(string schemeName)
        {
                        _schemeName = schemeName;
        }

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer", // Для Scalar/OpenAPI это значение ДОЛЖНО быть строчными буквами "bearer"
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Введите ваш JWT-токен"
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes.Add(_schemeName, securityScheme);

            var schemeReference = new OpenApiSecuritySchemeReference(_schemeName, document);

            return Task.CompletedTask;
        }
    }
}
