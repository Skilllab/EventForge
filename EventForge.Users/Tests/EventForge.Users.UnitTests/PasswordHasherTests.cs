using EventForge.Users.Infrastructure.Services;

using FluentAssertions;

namespace EventForge.Users.UnitTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    [Trait("Category", "PasswordHashing")]
    public void HashPassword_Should_Return_Deterministic_Hash()
    {
        // Arrange
        var hash1 = _sut.HashPassword("password123");
        var hash2 = _sut.HashPassword("password123");

        // Act & Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrWhiteSpace();
        hash1.Length.Should().Be(64);
    }

    [Fact]
    [Trait("Category", "PasswordHashing")]
    public void VerifyPassword_Should_Return_True_For_Matching_Password_And_Hash()
    {
        // Arrange
        var hash = _sut.HashPassword("password123");

        // Act
        var result = _sut.VerifyPassword("password123", hash);
        
        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "PasswordHashing")]
    public void VerifyPassword_Should_Return_False_For_Non_Matching_Password()
    {
        // Arrange
        var hash = _sut.HashPassword("password123");
        
        // Act
        var result = _sut.VerifyPassword("other-password", hash);
        
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "PasswordHashing")]
    public void HashPassword_Should_Throw_When_Password_Is_Empty()
    {
        // Arrange & Act  
        Action act = () => _sut.HashPassword(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("password");
    }

    [Fact]
    [Trait("Category", "PasswordHashing")]
    public void VerifyPassword_Should_Throw_When_Password_Is_Empty()
    {
        // Arrange
        var hash = _sut.HashPassword("password123");

        // Act
        Action act = () => _sut.VerifyPassword(string.Empty, hash);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("password");
    }

    [Fact]
    [Trait("Category", "PasswordHashing")]
    public void VerifyPassword_Should_Throw_When_Hash_Is_Empty()
    {
        // Arrange & Act    
        Action act = () => _sut.VerifyPassword("password123", string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("hash");
    }
}
