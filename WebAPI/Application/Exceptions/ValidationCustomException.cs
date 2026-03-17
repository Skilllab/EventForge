namespace WebAPI.Application.Exceptions
{
    /// <summary>
    /// Исключение, которое выбрасывается в случае если у сущности есть ошибки валидации 
    /// </summary>
    public class ValidationCustomException : ApplicationException
    {
        /// <summary>
        /// Тип сущности
        /// </summary>
        public string EntityName { get; }

        /// <summary>
        /// Идентификатор сущности
        /// </summary>
        public string EntityId { get; }

        public ValidationCustomException(string entityName, object entityId) : base($"Элемент {entityName} c ID: '{entityId}' имеет ошибки валидации.")
        {
            EntityName = entityName;
            EntityId = entityId.ToString();
        }

        public ValidationCustomException(string entityName, object entityId, string message) : base(message)
        {
            EntityName = entityName;
            EntityId = entityId.ToString();
        }
    }
}
