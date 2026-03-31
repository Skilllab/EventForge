using System.ComponentModel.DataAnnotations;

using EventBookingService.WebAPI.Application.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace EventBookingService.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class BookingsController (IBookingService bookingService, ILogger<BookingsController> logger) : ControllerBase
    {
        /// <summary>
        /// Получить информацию по бронированию
        /// </summary>
        [HttpGet("{bookingId:guid}")]
        [Tags("API для бронирования")]
        public async Task<IActionResult> GetBooking([Required]Guid bookingId, CancellationToken ct)
        {
            var bookingInfo = await bookingService.GetBookingByIdAsync(bookingId, ct);          

            return Ok(bookingInfo);
        }
    }
}
