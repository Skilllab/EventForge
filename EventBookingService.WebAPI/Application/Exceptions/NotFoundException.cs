namespace EventBookingService.WebAPI.Application.Exceptions
{
    /// <summary>
    /// Исключение, которое выбрасывается в случае если сущность не была найдена 
    /// </summary>
    public class NotFoundException : ApplicationBaseException
    {
        /// <inheritdoc />
        public NotFoundException(string entityName, object entityId)
            : base($"Элемент {entityName} c ID: '{entityId}' не найден.", entityName, entityId.ToString()) { }

        /// <inheritdoc />
        public NotFoundException(string entityName, object entityId, string message)
            : base(message, entityName, entityId.ToString()) { }
    }
}
