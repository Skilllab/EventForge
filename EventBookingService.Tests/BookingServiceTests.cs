using EventBookingService.WebAPI.Application.Exceptions;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Application.Services;
using EventBookingService.WebAPI.Models.Domain;
using EventBookingService.WebAPI.Models.DTO;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventBookingService.Tests
{
    public class BookingServiceTests
    {
        [Fact]
        [Trait("Category", "CreateBooking")]
        public async Task CreateBookingAsync_ShouldSaveBooking_WhenEventExists()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            var eventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            var eventDto = new ResponseEventDTO { Id = eventId, Title = "Test Event" };

            //Мокаем именно получение события, а не создание, потому что именно этот метод "GetEventAsync" участвует в методе "CreateBookingAsync"
            eventServiceMock
                .Setup(s => s.GetEventAsync(eventId, ct))
                .ReturnsAsync(eventDto);

            // Act
            await service.CreateBookingAsync(eventId, ct);

            // Assert
            repositoryMock.Verify(r => r.AddAsync(It.Is<Booking>(b => b.EventId == eventId), ct), Times.Once);
            eventServiceMock.Verify(s => s.GetEventAsync(eventId, ct), Times.Once);
        }

        [Fact]
        [Trait("Category", "CreateBooking")]
        public async Task CreateBookingAsync_ShouldThrowNotFound_WhenEventDoesNotExists()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            var eventId = Guid.NewGuid();
            var ct = CancellationToken.None;

            //Мокаем именно получение события, а не создание, потому что именно этот метод "GetEventAsync" участвует в методе "CreateBookingAsync"
            eventServiceMock
                .Setup(s => s.GetEventAsync(eventId, ct))
                .ThrowsAsync(new NotFoundException("Event", eventId));

            // Act
            Func<Task> act = async () => await service.CreateBookingAsync(eventId, ct);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "CreateBooking")]
        public async Task CreateBookingAsync_ShouldThrowIfCancelled()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            Func<Task> act = async () => await service.CreateBookingAsync(Guid.NewGuid(), cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        [Trait("Category", "GetBooking")]
        public async Task GetBookingByIdAsync_ShouldGetFromRepository()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            var bookingId = Guid.NewGuid();

            // Act
            await service.GetBookingByIdAsync(bookingId, CancellationToken.None);

            // Assert
            repositoryMock.Verify(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetBooking")]
        public async Task GetBookingByIdAsync_ShouldThrowIfCancelled()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(eventServiceMock.Object, repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var act = async () => await service.GetBookingByIdAsync(Guid.NewGuid(), cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
