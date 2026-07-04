using System.IdentityModel.Tokens.Jwt;

using EventForge.Settings.JWT;
using EventForge.Users.Infrastructure.Services;

using FluentAssertions;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace EventForge.Users.UnitTests;

public class JwtTokenGeneratorTests
{
    [Fact]
    [Trait("Category", "Jwt")]
    public void GenerateToken_Should_Create_Token_With_Expected_Claims_And_Metadata()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2025, 7, 1, 10, 30, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(now);
        var options = Options.Create(new JwtSettings
        {
            Secret = "super-secret-key-with-sufficient-length-12345",
            Issuer = "eventforge-users",
            Audience = "eventforge-clients",
            Lifetime = 2,
            SchemeName = null,
        });
        var sut = new JwtTokenGenerator(options, fakeTimeProvider);

        // Act
        var token = sut.GenerateToken(userId, "Admin");
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        jwt.Subject.Should().Be(userId.ToString());
        jwt.Claims.First(x => x.Type == "role").Value.Should().Be("Admin");
        jwt.Issuer.Should().Be("eventforge-users");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("eventforge-clients");
        jwt.ValidFrom.Should().Be(now.UtcDateTime);
        jwt.ValidTo.Should().Be(now.AddHours(2).UtcDateTime);
        jwt.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrWhiteSpace(x.Value));
    }
}
