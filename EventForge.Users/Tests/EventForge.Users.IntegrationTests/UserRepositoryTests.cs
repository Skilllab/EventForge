using EventForge.Shared.Enums;
using EventForge.Users.Domain.Entities;
using EventForge.Users.Infrastructure.Entities;
using EventForge.Users.Infrastructure.Repositories;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Users.IntegrationTests;

public class UserRepositoryTests : BaseRepositoryTest
{
    private static UserEntity CreateUserEntity(User user)
    {
        return new UserEntity
        {
            Id = user.Id,
            Login = user.Login,
            PasswordHash = user.PasswordHash,
            Role = user.Role,
        };
    }

    [Fact]
    public async Task GetByLoginAsync_Should_Return_User_When_User_Exists()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContext();
        var user = User.Create("AdminUser", "hashed_password", RoleType.Admin);

        context.Users.Add(CreateUserEntity(user));
        await context.SaveChangesAsync();

        var repository = new UserRepository(Factory);

        // Act
        var result = await repository.GetByLoginAsync("AdminUser");
        
        // Assert   
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Login.Should().Be("AdminUser");
        result.Role.Should().Be(RoleType.Admin);
    }

    [Fact]
    public async Task AddAsync_Should_Save_User_To_Database()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = await CreateContext();
        var repository = new UserRepository(Factory);
        var user = User.Create("NewUser", "hashed_password", RoleType.User);
        
        // Act
        await repository.AddAsync(user);
        var savedUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Login == "NewUser");

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.Id.Should().Be(user.Id);
        savedUser.PasswordHash.Should().Be("hashed_password");
        savedUser.Role.Should().Be(RoleType.User);
    }

    [Fact]
    public async Task AddAsync_Should_Throw_When_Login_Already_Exists()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = new UserRepository(Factory);
        var firstUser = User.Create("DuplicateUser", "hash1", RoleType.User);
        var secondUser = User.Create("DuplicateUser", "hash2", RoleType.Admin);

        // Act
        await repository.AddAsync(firstUser);
        Func<Task> act = async () => await repository.AddAsync(secondUser);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_Expected_Flag_Based_On_Stored_Login()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = new UserRepository(Factory);
        var user = User.Create("KnownUser", "hash", RoleType.User);

        await repository.AddAsync(user);

        //  Act
        var exists = await repository.ExistsAsync("KnownUser");
        var missing = await repository.ExistsAsync("UnknownUser");

        // Assert
        exists.Should().BeTrue();
        missing.Should().BeFalse();
    }
}
