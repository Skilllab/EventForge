using EventBookingService.Domain.Entities;
using EventBookingService.Infrastructure.Mapping;
using EventBookingService.Infrastructure.Repositories;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

namespace EventBookingService.IntegrationTests
{
    public class UserRepositoryTests : BaseRepositoryTest
    {
        [Fact]
        public async Task GetByIdAsync_Should_Return_User_When_User_Exists()
        {
            // Arrange
            await using var context = await CreateContext();
            var user = User.Create("Тестировщик", "hashed_password", RoleType.User);
            var userEntity = user.ToEntity();
            context.Users.Add(userEntity);
            await context.SaveChangesAsync();

            var repository = new UserRepository(Factory);

            // Act
            var result = await repository.GetByIdAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            result.Login.Should().Be("Тестировщик");
            result.Role.Should().Be(RoleType.User);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_User_Does_Not_Exist()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = await CreateContext();
            var repository = new UserRepository(Factory);
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await repository.GetByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByLoginAsync_Should_Return_User_When_User_Exists()
        {
            // Arrange
            await using var context = await CreateContext();
            var user = User.Create("Тестировщик", "hashed_password", RoleType.Admin);
            var userEntity = user.ToEntity();
            context.Users.Add(userEntity);
            await context.SaveChangesAsync();

            var repository = new UserRepository(Factory);

            // Act
            var result = await repository.GetByLoginAsync("Тестировщик");

            // Assert
            result.Should().NotBeNull();
            result!.Login.Should().Be("Тестировщик");
            result.Role.Should().Be(RoleType.Admin);
        }

        [Fact]
        public async Task GetByLoginAsync_Should_Return_Null_When_User_Does_Not_Exist()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = await CreateContext();
            var repository = new UserRepository(Factory);

            // Act
            var result = await repository.GetByLoginAsync("неТестировщик");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_Should_Save_User_To_Database()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = await CreateContext();
            var repository = new UserRepository(Factory);
            var user = User.Create("Тестировщик", "hashed_password", RoleType.User);

            // Act
            await repository.AddAsync(user);

            // Assert
            var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Login == "Тестировщик");
            savedUser.Should().NotBeNull();
            savedUser!.Login.Should().Be("Тестировщик");
            savedUser.Role.Should().Be("User");
        }

        [Fact]
        public async Task AddAsync_Should_Throw_When_Login_Already_Exists()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = await CreateContext();
            var repository = new UserRepository(Factory);
            var user1 = User.Create("Тестировщик", "hashed_password1", RoleType.User);
            var user2 = User.Create("Тестировщик", "hashed_password2", RoleType.User);

            // Act & Assert
            await repository.AddAsync(user1);

            // Попытка добавить пользователя с тем же логином должна выбросить исключение
            await Assert.ThrowsAsync<DbUpdateException>(async () => await repository.AddAsync(user2));
        }

        [Fact]
        public async Task ExistsAsync_Should_Return_True_When_User_Exists()
        {
            // Arrange
            await using var context = await CreateContext();
            var user = User.Create("Тестировщик", "hashed_password", RoleType.User);
            var userEntity = user.ToEntity();
            context.Users.Add(userEntity);
            await context.SaveChangesAsync();

            var repository = new UserRepository(Factory);

            // Act
            var result = await repository.ExistsAsync("Тестировщик");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_Should_Return_False_When_User_Does_Not_Exist()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = await CreateContext();
            var repository = new UserRepository(Factory);

            // Act
            var result = await repository.ExistsAsync("Тестировщик");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsAsync_Should_Be_Case_Sensitive()
        {
            // Arrange
            await using var context = await CreateContext();
            var user = User.Create("Тестировщик", "hashed_password", RoleType.User);
            var userEntity = user.ToEntity();
            context.Users.Add(userEntity);
            await context.SaveChangesAsync();

            var repository = new UserRepository(Factory);

            // Act
            var resultExact = await repository.ExistsAsync("Тестировщик");
            var resultDifferentCase = await repository.ExistsAsync("тестировщик");

            // Assert
            resultExact.Should().BeTrue();
            resultDifferentCase.Should().BeFalse();
        }

        [Fact]
        public async Task Multiple_Users_Should_Be_Stored_Independently()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = await CreateContext();
            var repository = new UserRepository(Factory);
            var user1 = User.Create("Тестировщик1", "password1", RoleType.User);
            var user2 = User.Create("Тестировщик2", "password2", RoleType.Admin);

            // Act
            await repository.AddAsync(user1);
            await repository.AddAsync(user2);

            // Assert
            var retrievedUser1 = await repository.GetByLoginAsync("Тестировщик1");
            var retrievedUser2 = await repository.GetByLoginAsync("Тестировщик2");

            retrievedUser1.Should().NotBeNull();
            retrievedUser1!.Role.Should().Be(RoleType.User);

            retrievedUser2.Should().NotBeNull();
            retrievedUser2!.Role.Should().Be(RoleType.Admin);

            retrievedUser1.Id.Should().NotBe(retrievedUser2.Id);
        }
    }
}
