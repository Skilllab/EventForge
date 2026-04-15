namespace EventBookingService.WebAPI.Application.Exceptions
{
    /// <summary>
    /// Исключение, которое выбрасывается в случае если у события нет мест для бронирования
    /// </summary>
    public class NoAvailableSeatsException : ApplicationBaseException
    {
        /// <inheritdoc />
        public NoAvailableSeatsException(string message, object entityName, string entityId)
            : base($"Для элемента {entityName} c ID: {entityId} нет доступных мест для бронирования", entityName, entityId) { }

        /// <inheritdoc />
        public NoAvailableSeatsException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
