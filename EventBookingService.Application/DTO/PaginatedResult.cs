namespace EventBookingService.Application.DTO;

/// <summary>
/// Перечень событий с фильтрацией и пагинацией
/// </summary>
/// <remarks>
/// Перечень событий с фильтрацией и пагинацией
/// </remarks>
/// <param name="eventsTotalCount">Общее количество событий</param>
/// <param name="events">Список событий</param>
/// <param name="currentPageNumber">Номер текущей страницы</param>
/// <param name="eventsCountOnCurrentPage">Количество элементов на текущей странице</param>
public class PaginatedResult(long eventsTotalCount,
    List<ResponseEventDTO> events,
    int currentPageNumber,
    int eventsCountOnCurrentPage)
{

    /// <summary>
    /// Общее количество событий, найденных по фильтру
    /// </summary>
    public long EventsTotalCount { get; set; } = eventsTotalCount;

    /// <summary>
    /// Список событий
    /// </summary>
    public List<ResponseEventDTO> Events { get; set; } = events;

    /// <summary>
    /// Номер текущей страницы
    /// </summary>
    public int CurrentPageNumber { get; set; } = currentPageNumber;

    /// <summary>
    /// Количество элементов на текущей странице
    /// </summary>
    public int EventsCountOnCurrentPage { get; set; } = eventsCountOnCurrentPage;
}
