using Microsoft.Extensions.DependencyInjection;

namespace EventForge.CQRS
{
    /// <summary>
    /// Класс для отправки команд и запросов в CQRS
    /// </summary>
    public sealed class Mediator(IServiceProvider serviceProvider) : ISender
    {
        /// <summary>
        /// Отправляет команду или запрос и возвращает результат выполнения
        /// </summary>
        /// <typeparam name="TResponse">Тип возвращаемого результата</typeparam>
        /// <param name="request">Команда или запрос для отправки</param>
        /// <param name="ct">Токен отмены</param>
        /// <returns>Результат выполнения команды или запроса</returns>
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        {
            var requestType = request.GetType();
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
            var handler = serviceProvider.GetRequiredService(handlerType);

            return ((dynamic) handler).Handle((dynamic) request, ct);
        }
    }
}
