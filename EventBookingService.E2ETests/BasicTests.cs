using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using EventBookingService.Domain.Entities;
using EventBookingService.Presentation.DTO;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace EventBookingService.E2ETests
{

    public class BasicTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        public BasicTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }
        /// <summary>
        /// Создает HTTP-запрос с фейковым телом для методов POST/PUT, 
        /// чтобы избежать ошибок валидации модели (400 Bad Request) до проверки авторизации.
        /// </summary>
        private static HttpRequestMessage CreateRequestWithEmptyBody(string url, string httpMethod)
        {
            var method = new HttpMethod(httpMethod);
            var request = new HttpRequestMessage(method, url);

            if (method == HttpMethod.Post || method == HttpMethod.Put)
            {
                // Отправляем пустой объект, так как для проверки 401/403 валидность данных не важна
                request.Content = JsonContent.Create(new { });
            }

            return request;
        }
        private record AuthResponse(string Token);


        /// <summary>
        /// Регистрирует нового пользователя с ролью User и возвращает его JWT-токен.
        /// </summary>
        private async Task<string> GetJwtTokenForRegularUserAsync(HttpClient client)
        {
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
            var userLogin = $"e2e_user_{uniqueSuffix}";
            var userPassword = "string";

            // 1. Регистрация
            var registerResponse = await client.PostAsJsonAsync("/auth/register", new CreateUserRequest
            {
                Login = userLogin,
                Password = userPassword,
                Role = nameof(RoleType.User) // Замените на nameof(RoleType.User), если доступно в контексте теста
            });
            registerResponse.IsSuccessStatusCode.Should().BeTrue();

            // 2. Логин
            var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginDataRequest
            {
                Login = userLogin,
                Password = userPassword
            });
            loginResponse.IsSuccessStatusCode.Should().BeTrue();

            var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            authResult.Should().NotBeNull();

            return authResult!.Token;
        }


        /// <summary>
        /// Проверяет, что защищенные эндпоинты возвращают 401 Unauthorized,
        /// если в запросе отсутствует JWT-токен.
        /// </summary>
        [Theory]
        [InlineData("/Events", "POST")]
        [InlineData("/Events/00000000-0000-0000-0000-000000000000", "PUT")]
        [InlineData("/Events/00000000-0000-0000-0000-000000000000", "DELETE")]
        [InlineData("/Events/00000000-0000-0000-0000-000000000000/book", "POST")]
        public async Task ProtectedEndpoints_Return401_WithoutToken(string url, string httpMethod)
        {
            // Arrange
            var client = _factory.CreateClient();

            using var request = CreateRequestWithEmptyBody(url, httpMethod);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Проверяет, что эндпоинты только для администраторов возвращают 403 Forbidden,
        /// если к ним обращается обычный пользователь (роль User).
        /// </summary>
        [Theory]
        [InlineData("/Events", "POST")]
        [InlineData("/Events/00000000-0000-0000-0000-000000000000", "PUT")]
        [InlineData("/Events/00000000-0000-0000-0000-000000000000", "DELETE")]
        public async Task AdminEndpoints_Return403_WhenUserIsNotAdmin(string url, string httpMethod)
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await GetJwtTokenForRegularUserAsync(client);

            // Добавляем using здесь тоже
            using var request = CreateRequestWithEmptyBody(url, httpMethod);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

}
