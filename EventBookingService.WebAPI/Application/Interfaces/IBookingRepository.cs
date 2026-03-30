using EventBookingService.WebAPI.Models.Domain;

namespace EventBookingService.WebAPI.Application.Interfaces
{
    /// <summary>
    /// Основной интерфейс репозитория бронирования
    /// </summary>
    public interface IBookingRepository
    {
        /// <summary>
        /// Добавление бронирования
        /// </summary>
        /// <param name="booking">Само бронирование</param>
        Task AddAsync(Booking booking, CancellationToken ct);

        /// <summary>
        /// Удаление бронирования
        /// </summary>
        /// <param name="id">ID бронирования</param>
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);

        /// <summary>
        /// Получение бронирования по ID
        /// </summary>
        /// <param name="id">ID бронирования</param>
        Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct);

        /// <summary>
        /// Получение всех бронирований и возврат как AsQueryable, чтобы сервис мог накладывать фильтры
        /// </summary>
        IQueryable<Booking> GetAll(CancellationToken ct);

        /// <summary>
        /// Обновление бронирования
        /// </summary>
        /// <param name="booking">Само бронирования</param>
        Task UpdateAsync(Booking booking, CancellationToken ct);
    }
}
