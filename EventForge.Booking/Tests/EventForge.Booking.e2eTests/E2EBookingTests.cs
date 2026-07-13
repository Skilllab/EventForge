using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

using EventForge.Booking.Application.DTO;
using EventForge.Booking.Infrastructure.Context;
using EventForge.Shared.Enums;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Testcontainers.PostgreSql;

namespace EventForge.Booking.e2eTests;

public class E2EBookingTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventforge_booking_e2e")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

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
                        ["JwtSettings:SchemeName"] = "Bearer",
                        ["BookingOptions:MaxBookingCount"] = "3"
                    });
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    // ========================================================================
    // Auth helpers
    // ========================================================================

    private static string GenerateToken(Guid userId, RoleType role)
    {

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("role", role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: JwtIssuer, audience: JwtAudience,
            claims: claims, notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetAuth(Guid userId, RoleType role)
        => _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(userId, role));

    private void SetAdminAuth(Guid userId)
        => SetAuth(userId, RoleType.Admin);

    private void SetUserAuth(Guid userId)
        => SetAuth(userId, RoleType.User);

    private void ClearAuth()
        => _client.DefaultRequestHeaders.Authorization = null;

    private static Guid NewUserId() => Guid.NewGuid();

    private async Task ResetDatabaseAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BookingDbContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    // ========================================================================
    // POST /bookings/{eventId} — CreateBooking
    // ========================================================================

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBooking_Should_Return_Accepted_When_Valid_Token()
    {
        await ResetDatabaseAsync();
        var userId = NewUserId();
        SetUserAuth(userId);

        var response = await _client.PostAsync($"/bookings/{Guid.NewGuid()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<BookingInfoDTO>();
        body.Should().NotBeNull();
        body!.ID.Should().NotBeEmpty();
        body.Status.Should().Be("Pending");
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBooking_Should_Return_Unauthorized_When_No_Token()
    {
        await ResetDatabaseAsync();
        ClearAuth();

        var response = await _client.PostAsync($"/bookings/{Guid.NewGuid()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBooking_Should_Fail_When_MaxBookingCount_Exceeded()
    {
        await ResetDatabaseAsync();
        var userId = NewUserId();
        SetUserAuth(userId);
        var eventId = Guid.NewGuid();

        for (var i = 0; i < 3; i++)
        {
            var ok = await _client.PostAsync($"/bookings/{eventId}", null);
            ok.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        var response = await _client.PostAsync($"/bookings/{eventId}", null);
        response.StatusCode.Should().NotBe(HttpStatusCode.Accepted);
    }


    [Fact]
    [Trait("Category", "GetBooking")]
    public async Task GetBooking_Should_Return_Ok_When_Booking_Exists()
    {
        await ResetDatabaseAsync();
        var userId = NewUserId();
        SetUserAuth(userId);
        var eventId = Guid.NewGuid();

        var createResponse = await _client.PostAsync($"/bookings/{eventId}", null);
        var created = await createResponse.Content.ReadFromJsonAsync<BookingInfoDTO>();

        var response = await _client.GetAsync($"/bookings/{created!.ID}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BookingInfoDTO>();
        body.Should().NotBeNull();
        body!.ID.Should().Be(created.ID);
        body.EventID.Should().Be(eventId);
        body.Status.Should().Be("Pending");
    }

    [Fact]
    [Trait("Category", "GetBooking")]
    public async Task GetBooking_Should_Return_NotFound_When_Booking_Does_Not_Exist()
    {
        await ResetDatabaseAsync();
        SetUserAuth(NewUserId());

        var response = await _client.GetAsync($"/bookings/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "GetBooking")]
    public async Task GetBooking_Should_Return_Unauthorized_When_No_Token()
    {
        await ResetDatabaseAsync();
        ClearAuth();

        var response = await _client.GetAsync($"/bookings/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Fact]
    [Trait("Category", "GetAllBooking")]
    public async Task GetAllBooking_Should_Return_Ok_With_User_Bookings_When_Admin()
    {
        await ResetDatabaseAsync();
        var userId = NewUserId();
        SetUserAuth(userId);
        var eventId = Guid.NewGuid();

        await _client.PostAsync($"/bookings/{eventId}", null);

        SetAdminAuth(userId);
        var response = await _client.GetAsync("/bookings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<BookingInfoDTO>>();
        body.Should().NotBeNull();
        body.Should().HaveCount(1);
        body![0].EventID.Should().Be(eventId);
    }

    [Fact]
    [Trait("Category", "GetAllBooking")]
    public async Task GetAllBooking_Should_Return_Empty_List_When_Admin_Has_No_Bookings()
    {
        await ResetDatabaseAsync();
        SetAdminAuth(NewUserId());

        var response = await _client.GetAsync("/bookings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<BookingInfoDTO>>();
        body.Should().NotBeNull();
        body.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "GetAllBooking")]
    public async Task GetAllBooking_Should_Return_Forbidden_When_Not_Admin()
    {
        await ResetDatabaseAsync();
        SetUserAuth(NewUserId());

        var response = await _client.GetAsync("/bookings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "GetAllBooking")]
    public async Task GetAllBooking_Should_Return_Unauthorized_When_No_Token()
    {
        await ResetDatabaseAsync();
        ClearAuth();

        var response = await _client.GetAsync("/bookings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ========================================================================
    // DELETE /bookings/{bookingId} — CancelBooking
    // ========================================================================

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Return_NoContent_When_Owner_Cancels_Own_Booking()
    {
        await ResetDatabaseAsync();
        var userId = NewUserId();
        SetUserAuth(userId);
        var eventId = Guid.NewGuid();

        var createResponse = await _client.PostAsync($"/bookings/{eventId}", null);
        var created = await createResponse.Content.ReadFromJsonAsync<BookingInfoDTO>();

        var response = await _client.DeleteAsync($"/bookings/{created!.ID}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Return_NoContent_When_Admin_Cancels_Other_Users_Booking()
    {
        await ResetDatabaseAsync();
        var ownerId = NewUserId();
        SetUserAuth(ownerId);
        var eventId = Guid.NewGuid();

        var createResponse = await _client.PostAsync($"/bookings/{eventId}", null);
        var created = await createResponse.Content.ReadFromJsonAsync<BookingInfoDTO>();

        var adminId = NewUserId();
        SetAdminAuth(adminId);
        var response = await _client.DeleteAsync($"/bookings/{created!.ID}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Return_Forbidden_When_Other_User_Tries_To_Cancel()
    {
        await ResetDatabaseAsync();
        var ownerId = NewUserId();
        SetUserAuth(ownerId);
        var eventId = Guid.NewGuid();

        var createResponse = await _client.PostAsync($"/bookings/{eventId}", null);
        var created = await createResponse.Content.ReadFromJsonAsync<BookingInfoDTO>();

        var otherUserId = NewUserId();
        SetUserAuth(otherUserId);
        var response = await _client.DeleteAsync($"/bookings/{created!.ID}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Return_NotFound_When_Booking_Does_Not_Exist()
    {
        await ResetDatabaseAsync();
        SetUserAuth(NewUserId());

        var response = await _client.DeleteAsync($"/bookings/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Return_Unauthorized_When_No_Token()
    {
        await ResetDatabaseAsync();
        ClearAuth();

        var response = await _client.DeleteAsync($"/bookings/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
