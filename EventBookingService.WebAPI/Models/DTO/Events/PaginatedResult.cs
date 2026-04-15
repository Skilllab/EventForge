namespace EventBookingService.WebAPI.Models.DTO.Events
{
    /// <summary>
    /// Перечень событий с фильтрацией и пагинацией
    /// </summary>
    public class PaginatedResult
    {
        /// <summary>
        /// Перечень событий с фильтрацией и пагинацией
        /// </summary>
        /// <param name="eventsTotalCount">Общее количество событий</param>
        /// <param name="events">Список событий</param>
        /// <param name="currentPageNumber">Номер текущей страницы</param>
        /// <param name="eventsCountOnCurrentPage">Количество элементов на текущей странице</param>
        public PaginatedResult(long eventsTotalCount,
            List<ResponseEventDTO> events,
            int currentPageNumber,
            int eventsCountOnCurrentPage)
        {
            EventsTotalCount = eventsTotalCount;
            Events = events;
            CurrentPageNumber = currentPageNumber;
            EventsCountOnCurrentPage = eventsCountOnCurrentPage;
        }

        /// <summary>
        /// Общее количество событий
        /// </summary>
        public long EventsTotalCount { get; set; }

        /// <summary>
        /// Список событий
        /// </summary>
        public List<ResponseEventDTO> Events { get; set; }

        /// <summary>
        /// Номер текущей страницы
        /// </summary>
        public int CurrentPageNumber { get; set; } = 1;

        /// <summary>
        /// Количество элементов на текущей странице
        /// </summary>
        public int EventsCountOnCurrentPage { get; set; }
    }
}
