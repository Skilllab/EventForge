namespace EventBookingService.WebAPI.Models.DTO
{
    /// <summary>
    /// Фильтр получения событий
    /// </summary>
    public class EventsFilter
    {
        /// <summary>
        /// Название события
        /// </summary>
        public string? title { get; set; }

        /// <summary>
        /// Дата начала события
        /// </summary>
        public DateTime? from { get; set; }

        /// <summary>
        /// Дата завершения события
        /// </summary>
        public DateTime? to { get; set; }

        /// <summary>
        /// Страница, которую необходимо вернуть
        /// </summary>
        public int page { get; set; } = 1;

        /// <summary>
        /// Количество элементов на странице
        /// </summary>
        public int pageSize { get; set; } = 10;
    }

}
