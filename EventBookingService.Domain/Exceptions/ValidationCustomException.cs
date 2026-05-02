namespace EventBookingService.Domain.Exceptions
{
    /// <summary>
    /// Исключение, которое выбрасывается в случае если у сущности есть ошибки валидации 
    /// </summary>
    public class ValidationCustomException : ApplicationBaseException
    {
        /// <inheritdoc />
        public ValidationCustomException(string entityName, object entityId)
            : base($"Элемент {entityName} c ID: '{entityId}' имеет ошибки валидации.", entityName, entityId.ToString()) { }

        /// <inheritdoc />
        public ValidationCustomException(string entityName, object entityId, string message)
            : base(entityName, entityId, message) { }
    }
}
