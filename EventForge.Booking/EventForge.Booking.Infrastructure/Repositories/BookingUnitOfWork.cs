using System;
using System.Collections.Generic;
using System.Text;

using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Infrastructure.Context;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Booking.Infrastructure.Repositories
{
    /// <summary>
    /// Выполняет несколько операций в одной транзакции BookingDbContext.
    /// </summary>
    public class BookingUnitOfWork(IDbContextFactory<BookingDbContext> factory) : IBookingUnitOfWork
    {
        public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
        {
            await using var context = await factory.CreateDbContextAsync(ct);
            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            try
            {
                await action(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
    }
}
