using EventBookingService.Data.Context;
using EventBookingService.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace EventBookingService.Data.Services;

/// <summary>
/// Сервис для управления транзакциями базы данных
/// </summary>
public class TransactionService(IDbContextFactory<AppDbContext> factory) : ITransactionService
{
    /// <inheritdoc/>
    public async Task<T> ExecuteAsync<T>(Func<ITransactionContext, Task<T>> operation, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var context = await factory.CreateDbContextAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            var transactionContext = new TransactionContext(context);
            var result = await operation(transactionContext);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(Func<ITransactionContext, Task> operation, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var context = await factory.CreateDbContextAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            var transactionContext = new TransactionContext(context);
            await operation(transactionContext);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
