namespace EventBookingService.Domain.Exceptions
{
    /// <summary>
    /// Исключение, которое выбрасывается в случае если у события нет мест для бронирования
    /// </summary>
    public class NoAvailableSeatsException : ApplicationBaseException
    {
        /// <inheritdoc />
        public NoAvailableSeatsException(string entityName, string entityId)
            : base($"Для элемента {entityName} c ID: {entityId} нет доступных мест для бронирования", entityName, entityId) { }

        /// <inheritdoc />
        /// <inheritdoc />
        public NoAvailableSeatsException(string entityName, object entityId, string message)
            : base(message, entityName, entityId.ToString()) { }
    }
}
