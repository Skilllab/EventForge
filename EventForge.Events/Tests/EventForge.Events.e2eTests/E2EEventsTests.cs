using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

using EventForge.Events.Infrastructure.Context;
using EventForge.Events.Presentation;
using EventForge.Events.Presentation.DTO;
using EventForge.Shared.Enums;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Testcontainers.PostgreSql;

namespace EventForge.Events.e2eTests;

public class E2EEventsTests : IAsyncLifetime
{

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventforge_events_e2e")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _anonymousClient = null!;

    // Константы для JWT (должны совпадать с appsettings)
    private const string JwtSecret = "SuperSecretKeyWithMoreThan32CharactersLength!!";
    private const string JwtIssuer = "EventBookingService";
    private const string JwtAudience = "EventBookingAPI";

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

#pragma warning disable CA2000
        _factory = new WebApplicationFactory<Program>()
#pragma warning restore CA2000
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTests");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                        ["JwtSettings:Secret"] = JwtSecret,
                        ["JwtSettings:Issuer"] = JwtIssuer,
                        ["JwtSettings:Audience"] = JwtAudience,
                        ["JwtSettings:Lifetime"] = "2",
                        ["JwtSettings:SchemeName"] = "Bearer"
                    });
                });
            });

        _anonymousClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _anonymousClient.Dispose();
        await _factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    #region Пререквизиты
    /// <summary>
    /// Генерирует JWT-токен с ролью Admin для авторизованных запросов
    /// </summary>
    private static string GenerateAdminToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("role", nameof(RoleType.Admin)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Создаёт HttpClient с заголовком Authorization Admin
    /// </summary>
    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateAdminToken());
        return client;
    }

    /// <summary>
    /// Генерирует токен с ролью User
    /// </summary>
    private static string GenerateUserToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("role", nameof(RoleType.User)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Создаёт HttpClient с заголовком Authorization User
    /// </summary>
    private HttpClient CreateUserClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateUserToken());
        return client;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EventsDbContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    #endregion


    #region POST /events — CreateEvent

    [Fact]
    [Trait("Category", "CreateEvent")]
    public async Task CreateEvent_Should_Return_Created_When_Admin_Provides_Valid_Data()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var client = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(1);
        var request = new CreateEventRequest
        {
            Title = "E2E конференция",
            StartAt = startAt,
            EndAt = startAt.AddHours(3),
            TotalSeats = 50,
            Description = "E2E тестовое событие"
        };

        // Act
        var response = await client.PostAsJsonAsync("/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<EventResponse>();
        body.Should().NotBeNull();
        body!.Title.Should().Be("E2E конференция");
        body.TotalSeats.Should().Be(50);
        body.AvailableSeats.Should().Be(50);
        body.Id.Should().NotBeEmpty();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EventsDbContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();
        var saved = await context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == body.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "CreateEvent")]
    public async Task CreateEvent_Should_Return_Unauthorized_When_No_Token()
    {
        // Arrange
        await ResetDatabaseAsync();
        var request = new CreateEventRequest
        {
            Title = "Не авторизирован",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 10
        };

        // Act
        var response = await _anonymousClient.PostAsJsonAsync("/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "CreateEvent")]
    public async Task CreateEvent_Should_Return_Forbidden_When_User_Is_Not_Admin()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var client = CreateUserClient();
        var request = new CreateEventRequest
        {
            Title = "Попытка пользователя",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 10
        };

        // Act
        var response = await client.PostAsJsonAsync("/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "CreateEvent")]
    public async Task CreateEvent_Should_Return_BadRequest_When_Title_Is_Missing()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var client = CreateAdminClient();
        var request = new CreateEventRequest
        {
            Title = "", // пустой заголовок
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 10
        };

        // Act
        var response = await client.PostAsJsonAsync("/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Category", "CreateEvent")]
    public async Task CreateEvent_Should_Return_BadRequest_When_EndAt_Before_StartAt()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var client = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(2);
        var request = new CreateEventRequest
        {
            Title = "Неверные даты",
            StartAt = startAt,
            EndAt = startAt.AddHours(-1), // конец раньше начала
            TotalSeats = 10
        };

        // Act
        var response = await client.PostAsJsonAsync("/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Category", "CreateEvent")]
    public async Task CreateEvent_Should_Return_BadRequest_When_TotalSeats_Is_Zero()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var client = CreateAdminClient();
        var request = new CreateEventRequest
        {
            Title = "Нулевое количество мест",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = 0 // невалидно
        };

        // Act
        var response = await client.PostAsJsonAsync("/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion


    #region GET /events/{id} — GetEvent

    [Fact]
    [Trait("Category", "GetEvent")]
    public async Task GetEvent_Should_Return_Ok_When_Event_Exists()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var adminClient = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(3);
        var createRequest = new CreateEventRequest
        {
            Title = "Просто событие",
            StartAt = startAt,
            EndAt = startAt.AddHours(2),
            TotalSeats = 30,
            Description = "Для чтения"
        };

        var createResponse = await adminClient.PostAsJsonAsync("/events", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        // Act — GET доступен анонимно
        var response = await _anonymousClient.GetAsync($"/events/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EventResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(created.Id);
        body.Title.Should().Be("Просто событие");
        body.AvailableSeats.Should().Be(30);
    }

    [Fact]
    [Trait("Category", "GetEvent")]
    public async Task GetEvent_Should_Return_NotFound_When_Event_Does_Not_Exist()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        var response = await _anonymousClient.GetAsync($"/events/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /events — GetAllEvents (пагинация + фильтрация)

    [Fact]
    [Trait("Category", "GetAllEvents")]
    public async Task GetAllEvents_Should_Return_Paginated_Results_With_Default_Params()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var client = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(5);

        // Создаём 3 события
        for (var i = 1; i <= 3; i++)
        {
            await client.PostAsJsonAsync("/events", new CreateEventRequest
            {
                Title = $"Событие {i}",
                StartAt = startAt.AddDays(i),
                EndAt = startAt.AddDays(i).AddHours(2),
                TotalSeats = 10 * i
            });
        }

        // Act
        var response = await _anonymousClient.GetAsync("/events?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaginatedResultResponse>();
        body.Should().NotBeNull();
        body!.EventsTotalCount.Should().Be(3);
        body.CurrentPageNumber.Should().Be(1);
        body.EventsCountOnCurrentPage.Should().Be(2);
        body.Events.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "GetAllEvents")]
    public async Task GetAllEvents_Should_Filter_By_Title()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var client = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(5);

        await client.PostAsJsonAsync("/events", new CreateEventRequest
        {
            Title = "Воркшоп без боли",
            StartAt = startAt,
            EndAt = startAt.AddHours(3),
            TotalSeats = 20
        });
        await client.PostAsJsonAsync("/events", new CreateEventRequest
        {
            Title = "Воркшоп с болью",
            StartAt = startAt.AddDays(1),
            EndAt = startAt.AddDays(1).AddHours(3),
            TotalSeats = 15
        });

        // Act — ищем только события с "без" в названии
        var response = await _anonymousClient.GetAsync("/events?title=без");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaginatedResultResponse>();
        body.Should().NotBeNull();
        body!.EventsTotalCount.Should().Be(1);
        body.Events.Should().ContainSingle();
        body.Events[0].Title.Should().Be("Воркшоп без боли");
    }

    [Fact]
    [Trait("Category", "GetAllEvents")]
    public async Task GetAllEvents_Should_Return_Empty_When_No_Events_Match()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        var response = await _anonymousClient.GetAsync("/events?title=nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaginatedResultResponse>();
        body.Should().NotBeNull();
        body!.EventsTotalCount.Should().Be(0);
        body.Events.Should().BeEmpty();
    }

    #endregion

    #region PUT /events/{id} — ChangeEvent
    [Fact]
    [Trait("Category", "ChangeEvent")]
    public async Task ChangeEvent_Should_Return_NoContent_When_Admin_Updates_Event()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var adminClient = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(4);

        var createResponse = await adminClient.PostAsJsonAsync("/events", new CreateEventRequest
        {
            Title = "Старое название",
            StartAt = startAt,
            EndAt = startAt.AddHours(2),
            TotalSeats = 25,
            Description = "Старое описание"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Новое название",
            Description = "Новое описание"
        };

        // Act
        var response = await adminClient.PutAsJsonAsync($"/events/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Проверяем через GET, что данные обновились
        var getResponse = await _anonymousClient.GetAsync($"/events/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<EventResponse>();
        updated!.Title.Should().Be("Новое название");
        updated.Description.Should().Be("Новое описание");
        updated.TotalSeats.Should().Be(25); // не менялось
    }

    [Fact]
    [Trait("Category", "ChangeEvent")]
    public async Task ChangeEvent_Should_Return_NotFound_When_Event_Does_Not_Exist()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var client = CreateAdminClient();
        var request = new UpdateEventRequest { Title = "Призрак" };

        // Act
        var response = await client.PutAsJsonAsync($"/events/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "ChangeEvent")]
    public async Task ChangeEvent_Should_Return_Unauthorized_When_No_Token()
    {
        // Arrange
        await ResetDatabaseAsync();
        var request = new UpdateEventRequest { Title = "Взлом" };

        // Act
        var response = await _anonymousClient.PutAsJsonAsync($"/events/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "ChangeEvent")]
    public async Task ChangeEvent_Should_Return_Forbidden_When_User_Is_Not_Admin()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var adminClient = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(4);
        var createResponse = await adminClient.PostAsJsonAsync("/events", new CreateEventRequest
        {
            Title = "Protected",
            StartAt = startAt,
            EndAt = startAt.AddHours(2),
            TotalSeats = 10
        });
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        using var userClient = CreateUserClient();
        var request = new UpdateEventRequest { Title = "Взлом" };

        // Act
        var response = await userClient.PutAsJsonAsync($"/events/{created!.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion



    #region DELETE /events/{id} — CancelEvent
    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEvent_Should_Return_NoContent_When_Admin_Deletes_Event()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var adminClient = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(6);

        var createResponse = await adminClient.PostAsJsonAsync("/events", new CreateEventRequest
        {
            Title = "Будет удалено",
            StartAt = startAt,
            EndAt = startAt.AddHours(1),
            TotalSeats = 5
        });
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        // Act
        var response = await adminClient.DeleteAsync($"/events/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Проверяем, что событие реально удалено
        var getResponse = await _anonymousClient.GetAsync($"/events/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEvent_Should_Return_NotFound_When_Event_Does_Not_Exist()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var client = CreateAdminClient();

        // Act
        var response = await client.DeleteAsync($"/events/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEvent_Should_Return_Unauthorized_When_No_Token()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        var response = await _anonymousClient.DeleteAsync($"/events/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEvent_Should_Return_Forbidden_When_User_Is_Not_Admin()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var adminClient = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(7);
        var createResponse = await adminClient.PostAsJsonAsync("/events", new CreateEventRequest
        {
            Title = "Защищённое удаление",
            StartAt = startAt,
            EndAt = startAt.AddHours(2),
            TotalSeats = 10
        });
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>();

        using var userClient = CreateUserClient();

        // Act
        var response = await userClient.DeleteAsync($"/events/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Событие всё ещё существует
        var getResponse = await _anonymousClient.GetAsync($"/events/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
