using EventBookingService.Application.Interfaces;
using EventBookingService.Domain.Entities;
using EventBookingService.Infrastructure.Context;
using EventBookingService.Infrastructure.Mapping;

using Microsoft.EntityFrameworkCore;

namespace EventBookingService.Infrastructure.Repositories
{
    public class UserRepository(IDbContextFactory<AppDbContext> factory) : IUserRepository
    {
        public async Task<User?> GetByIdAsync(Guid id)
        {
            await using var context = await factory.CreateDbContextAsync();
            var user =  await context.Users
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.Id == id);

            return user?.ToDomain();
        }

        public async Task<User?> GetByLoginAsync(string login)
        {
            await using var context = await factory.CreateDbContextAsync();

            var user = await context.Users
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.Login == login);

            return user?.ToDomain();
        }

        public async Task AddAsync(User user)
        {
            await using var context = await factory.CreateDbContextAsync();
            var entityUser = user.ToEntity();
            await context.Users.AddAsync(entityUser);
            await context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(string login)
        {
            await using var context = await factory.CreateDbContextAsync();

            return await context.Users.AnyAsync(u => u.Login == login);
        }
    }
}
