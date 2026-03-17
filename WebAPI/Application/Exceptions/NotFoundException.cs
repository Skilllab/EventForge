namespace WebAPI.Application.Exceptions
{
    /// <summary>
    /// Исключение, которое выбрасывается в случае если сущность не была найдена 
    /// </summary>
    public class NotFoundException : ApplicationException
    {
        /// <summary>
        /// Тип сущности
        /// </summary>
        public string EntityName { get; }

        /// <summary>
        /// Идентификатор сущности
        /// </summary>
        public object EntityId { get; }

        public NotFoundException(string entityName, object entityId) : base($"Элемент {entityName} c ID: '{entityId}' не найден.")
        {
            EntityName = entityName;
            EntityId = entityId;
        }

        public NotFoundException(string entityName, object entityId, string message) : base(message)
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }
}
