namespace EventBookingService.Application.Interfaces;

/// <summary>
/// Контекст для операций в рамках транзакции
/// </summary>
public interface ITransactionContext
{
    /// <summary>
    /// Объект контекста для выполнения операций (DbContext)
    /// </summary>
    object DbContext { get; }
}
