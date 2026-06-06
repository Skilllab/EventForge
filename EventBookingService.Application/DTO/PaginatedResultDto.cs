namespace EventBookingService.Application.DTO;

/// <remarks>
/// Перечень событий с фильтрацией и пагинацией
/// </remarks>
/// <param name="EventsTotalCount">Общее количество событий, найденных по фильтру</param>
/// <param name="Events">Список событий</param>
/// <param name="CurrentPageNumber">Номер текущей страницы</param>
/// <param name="EventsCountOnCurrentPage">Количество элементов на текущей странице</param>
public record PaginatedResultDto(
    long EventsTotalCount,
    List<EventDto> Events,
    int CurrentPageNumber,
    int EventsCountOnCurrentPage
);
