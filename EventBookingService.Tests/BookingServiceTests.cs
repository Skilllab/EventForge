using EventBookingService.WebAPI.Application.Exceptions;
using EventBookingService.WebAPI.Application.Interfaces;
using EventBookingService.WebAPI.Application.Services;
using EventBookingService.WebAPI.Infrastructure.Persistence;
using EventBookingService.WebAPI.Models.Domain;
using EventBookingService.WebAPI.Models.DTO.Events;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
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
            var service = new BookingService(repositoryMock.Object, loggerMock.Object);
            var eventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            var eventDto = new ResponseEventDTO { Id = eventId, Title = "Test Event" };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            //Мокаем именно получение события, а не создание, потому что именно этот метод "GetEventAsync" участвует в методе "CreateBookingAsync"
            eventServiceMock
                .Setup(s => s.GetEventAsync(eventId, ct))
                .ReturnsAsync(eventDto);

            // Act
            var result = await service.CreateBookingAsync(eventId, ct);

            // Assert
            result.Status.Should().Be(nameof(BookingStatus.Pending));
            repositoryMock.Verify(r => r.AddAsync(It.Is<Booking>(b => b.EventId == eventId), ct), Times.Once);
            eventServiceMock.Verify(s => s.GetEventAsync(eventId, ct), Times.Once);
        }

        [Fact]
        [Trait("Category", "CreateBooking")]
        public async Task CreateBookingAsync_RunManyTimes_ShouldSaveBooking_WhenEventExists()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(repositoryMock.Object, loggerMock.Object);
            var eventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            var eventDto = new ResponseEventDTO { Id = eventId, Title = "Test Event" };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            //Мокаем именно получение события, а не создание, потому что именно этот метод "GetEventAsync" участвует в методе "CreateBookingAsync"
            eventServiceMock
                .Setup(s => s.GetEventAsync(eventId, ct))
                .ReturnsAsync(eventDto);

            //Проверим множество значений добавляя всё в цикле. Уникальность гарантируем HashSet
            int count = 10;
            var createdIds = new HashSet<Guid>();

            // Act
            for (int i = 0; i < count; i++)
            {
                var result = await service.CreateBookingAsync(eventId, ct);
                createdIds.Add(result.ID);
            }


            // Assert
            createdIds.Should().HaveCount(count);
            repositoryMock.Verify(r => r.AddAsync(It.Is<Booking>(b => b.EventId == eventId), ct), Times.Exactly(count));
            eventServiceMock.Verify(s => s.GetEventAsync(eventId, ct), Times.Exactly(count));
        }

        [Fact]
        [Trait("Category", "CreateBooking")]
        public async Task CreateBookingAsync_ShouldThrowNotFound_WhenEventDoesNotExists()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(repositoryMock.Object, loggerMock.Object);
            var eventId = Guid.NewGuid();
            var ct = CancellationToken.None;
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            //Мокаем именно получение события, а не создание, потому что именно этот метод "GetEventAsync" участвует в методе "CreateBookingAsync"
            eventServiceMock
                .Setup(s => s.GetEventAsync(eventId, ct))
                .ThrowsAsync(new NotFoundException(nameof(Event), eventId));

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
            var service = new BookingService(repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()));

            // Act
            Func<Task> act = async () => await service.CreateBookingAsync(Guid.NewGuid(), cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "CreateBooking")]
        public async Task GetBookingByIdAsync_GetByNotExistedID_ShouldThrow_NotFound()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(repositoryMock.Object, loggerMock.Object);

            var nonExistentId = Guid.NewGuid();
            var ct = CancellationToken.None;

            // Настраиваем репозиторий на возврат null (объект не найден)
            repositoryMock
                .Setup(r => r.GetByIdAsync(nonExistentId, ct))
                .ReturnsAsync((Booking?) null);

            // Act
            // Используем Func для захвата исключения, так как метод асинхронный
            Func<Task> act = async () => await service.GetBookingByIdAsync(nonExistentId, ct);

            // Assert
            // Проверяем, что выброшено именно NotFoundException
            await act.Should().ThrowAsync<NotFoundException>(); // Проверка наличия ID в сообщении (если есть)

            // Проверяем, что репозиторий действительно опрашивался
            repositoryMock.Verify(r => r.GetByIdAsync(nonExistentId, ct), Times.Once);
        }


        [Fact]
        [Trait("Category", "GetBooking")]
        public async Task GetBookingByIdAsync_ShouldGetFromRepository()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(repositoryMock.Object, loggerMock.Object);
            var booking = Booking.Create(Guid.NewGuid(), DateTime.Now);
            repositoryMock.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

            // Act
            var result = await service.GetBookingByIdAsync(booking.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ID.Should().Be(booking.Id);
            repositoryMock.Verify(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetBooking")]
        public async Task GetBookingByIdAsync_ShouldThrowIfCancelled()
        {
            // Arrange
            var eventServiceMock = new Mock<IEventService>();
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingService>>();
            var service = new BookingService(repositoryMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()));


            // Act
            var act = async () => await service.GetBookingByIdAsync(Guid.NewGuid(), cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        }

        [Fact]
        public async Task ExecuteAsync_ShouldUpdateStatus_AndServiceShouldReturnUpdatedBooking()
        {
            // Arrange
            var repositoryMock = new Mock<IBookingRepository>();
            var loggerBgMock = new Mock<ILogger<BookingBackgroundService>>();
            var loggerSvcMock = new Mock<ILogger<BookingService>>();
            var eventServiceMock = new Mock<IEventService>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            // Настроим BackgroundService
            scopeFactoryMock.Setup(s => s.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceProviderMock.Setup(s => s.GetService(typeof(IBookingRepository))).Returns(repositoryMock.Object);

            var booking = Booking.Create(Guid.NewGuid(), DateTime.Now);

            // 1. Для фонового сервиса (поиск ожидающих)
            repositoryMock
                .Setup(r => r.GetAll(It.IsAny<Func<Booking, bool>>(), It.IsAny<CancellationToken>()))
                .Returns(new List<Booking> { booking });

            // 2. Для BookingService (получение по ID) — возвращаем тот же экземпляр
            repositoryMock
                .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            var backgroundService = new BookingBackgroundService(scopeFactoryMock.Object, loggerBgMock.Object);
            var bookingService = new BookingService(repositoryMock.Object, loggerSvcMock.Object);
            using var cts = new CancellationTokenSource();

            // Act
            await backgroundService.StartAsync(cts.Token);

            // Даем сервису время отработать
            await Task.Delay(2500);

            cts.Cancel();

            await backgroundService.StopAsync(CancellationToken.None);

            // Assert
            var resultDto = await bookingService.GetBookingByIdAsync(booking.Id, CancellationToken.None);

            resultDto.Status.Should().Be(nameof(BookingStatus.Confirmed));
            
            repositoryMock.Verify(r => r.UpdateAsync(
                It.Is<Booking>(b => b.Id == booking.Id && b.Status == BookingStatus.Confirmed),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }






    }
}
