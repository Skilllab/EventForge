namespace EventBookingService.Application.Interfaces;

/// <summary>
/// Сервис для управления транзакциями базы данных
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Выполнить операцию в рамках транзакции
    /// </summary>
    /// <param name="operation">Асинхронная операция для выполнения в транзакции</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат выполнения операции</returns>
    Task<T> ExecuteAsync<T>(Func<ITransactionContext, Task<T>> operation, CancellationToken ct = default);

    /// <summary>
    /// Выполнить операцию в рамках транзакции без возврата значения
    /// </summary>
    /// <param name="operation">Асинхронная операция для выполнения в транзакции</param>
    /// <param name="ct">Токен отмены</param>
    Task ExecuteAsync(Func<ITransactionContext, Task> operation, CancellationToken ct = default);
}
