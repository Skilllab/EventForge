using System.Reflection;
using System.Xml.XPath;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EventForge.Users.Presentation.OpenApi
{
    public class XmlCommentsDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            // 1. Находим путь к сгенерированному XML-файлу документации
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (!File.Exists(xmlPath)) return Task.CompletedTask;

            // 2. Загружаем XML структуру
            var doc = new XPathDocument(xmlPath);
            var nav = doc.CreateNavigator();

            // 3. Бежим по всем тегам, которые .NET автоматически создал для контроллеров
            foreach (var tag in document.Tags)
            {
                // Ищем класс контроллера, чьё имя совпадает с именем тега (например, AuthController)
                // И достаем его системный путь в XML (формат: T:Namespace.AuthController)
                string controllerTypeName = $"{Assembly.GetExecutingAssembly().GetName().Name}.Controllers.{tag.Name}Controller";

                // Если у вас контроллеры лежат в другой папке/namespace, подправьте строку выше под свой проект
                var memberNode = nav.SelectSingleNode($"/doc/members/member[@name='T:{controllerTypeName}']/summary");

                if (memberNode != null)
                {
                    // Автоматически вытаскиваем текст и очищаем его от лишних пробелов/переносов строк
                    tag.Description = memberNode.Value.Trim();
                }
            }

            return Task.CompletedTask;
        }
    }
}
