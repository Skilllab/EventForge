using EventBookingService.Data.Context;
using EventBookingService.Domain.Interfaces;

namespace EventBookingService.Data.Services;

/// <summary>
/// Контекст для операций в рамках транзакции
/// </summary>
internal class TransactionContext : ITransactionContext
{
    private readonly AppDbContext _dbContext;

    public TransactionContext(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public object DbContext => _dbContext;
}
