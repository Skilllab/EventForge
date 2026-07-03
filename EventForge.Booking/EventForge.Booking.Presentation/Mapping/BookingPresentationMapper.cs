using EventForge.Booking.Application.DTO;
using EventForge.Booking.Presentation.DTO;

namespace EventForge.Booking.Presentation.Mapping;

/// <summary>
/// Маппер для преобразования моделей между слоями Presentation и Application.
/// </summary>
public static class BookingPresentationMapper
{
    /// <summary>
    /// Преобразует выходной DTO бизнес-логики (Application) в ответ клиенту (Presentation).
    /// </summary>
    /// <param name="dto">DTO информации о бронировании</param>
    public static BookingInfoResponse ToWebDto(this BookingInfoDTO dto) =>
        new(
            ID: dto.ID,
            EventID: dto.EventID,
            Status: dto.Status);
}
