namespace EventBookingService.WebAPI.Application.Exceptions
{
    /// <summary>
    /// Базовый класс для кастом исключений
    /// </summary>
    public abstract class ApplicationBaseException : Exception
    {
        /// <summary>
        /// Тип сущности
        /// </summary>
        public string EntityName { get;  }

        /// <summary>
        /// Идентификатор сущности
        /// </summary>
        public string EntityId { get; }


        /// <summary>
        /// Конструктор исключения
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="entityName">Имя сущности</param>
        /// <param name="entityId">Идентификатор сущности</param>
        protected ApplicationBaseException(string message, object entityName, string entityId)
            : base(message)
        {
            EntityName = entityName.ToString();
            EntityId = entityId;
        }

        /// <inheritdoc />
        protected ApplicationBaseException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
