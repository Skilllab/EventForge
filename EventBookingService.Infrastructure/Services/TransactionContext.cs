using EventBookingService.Application.Interfaces;
using EventBookingService.Infrastructure.Context;

namespace EventBookingService.Infrastructure.Services;

/// <summary>
/// Контекст для операций в рамках транзакции с internal доступом к DbContext.
/// </summary>
internal sealed class TransactionContext(AppDbContext dbContext) : ITransactionContext
{
    internal AppDbContext DbContext { get; } = dbContext;
}
