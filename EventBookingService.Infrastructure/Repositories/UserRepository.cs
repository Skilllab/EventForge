using EventBookingService.Application.Interfaces;
using EventBookingService.Domain.Entities;
using EventBookingService.Infrastructure.Context;

using Microsoft.EntityFrameworkCore;

namespace EventBookingService.Infrastructure.Repositories
{
    public class UserRepository(IDbContextFactory<AppDbContext> factory) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id) => throw new NotImplementedException();

        public Task<User?> GetByLoginAsync(string login) => throw new NotImplementedException();

        public Task AddAsync(User user) => throw new NotImplementedException();

        public Task<bool> ExistsAsync(string login) => throw new NotImplementedException();
    }
}
