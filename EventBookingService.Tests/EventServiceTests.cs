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
    public class EventServiceTests
    {
        #region CreateEvent tests

        [Fact]
        [Trait("Category", "CreateEvent")]
        public void CreateEvent_ShouldReturnResponseEventDTO_WhenCreateEventDTOIsValid()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);

            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1)
            };

            // Act
            var result = service.CreateEvent(dto);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(dto, options => options
                .Including(x => x.Title)
                .Including(x => x.StartAt)
                .Including(x => x.EndAt));

            // Проверяем, что репозиторий действительно вызывался один раз
            repositoryMock.Verify(r => r.Add(It.IsAny<Event>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "CreateEvent")]
        public void CreateEvent_ShouldThrowValidationException_WhenCreateEventDTOAreInvalid()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие с невалидной моделью данных",
                StartAt = DateTime.Now.AddHours(2),
                EndAt = DateTime.Now.AddHours(1)
            };

            // Act
            Action act = () => service.CreateEvent(dto);

            // Assert
            act.Should().Throw<ValidationCustomException>();

            // Проверяем, что метод добавления в репозиторий не вызывался, так как данные невалидные
            repositoryMock.Verify(r => r.Add(It.IsAny<Event>()), Times.Never);
        }

        #endregion

        #region GetEvents tests

        [Fact]
        [Trait("Category", "GetEvents")]
        public void GetEvents_ShouldReturnPaginatedResultWithAllEvents()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filter = new EventsFilter();
            var fakeEvents = new List<Event>
            {
                Event.Create("тестовое событие 1", DateTime.Now, DateTime.Now.AddHours(1)),
                Event.Create("тестовое событие 34", DateTime.Now, DateTime.Now.AddHours(2)),
                Event.Create("тестовое событие 2", DateTime.Now, DateTime.Now.AddHours(3)),
            }.AsQueryable();

            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Act
            var result = service.GetEvents(filter);

            // Assert
            result.Should().NotBeNull();
            result.EventsTotalCount.Should().Be(3); // Проверяем общее количество
            result.Events.Should().HaveCount(3); // Проверяем количество в текущей выборке
            result.Events.Should().BeInAscendingOrder(e => e.Title); // Проверяем сортировку, которая есть в сервисе
            result.Events.Should().BeEquivalentTo(fakeEvents, options => options
                .Including(x => x.Title)
                .Including(x => x.StartAt)
                .Including(x => x.EndAt)
            );

            repositoryMock.Verify(r => r.GetAll(), Times.Once);
        }


        [Fact]
        [Trait("Category", "GetEvents")]
        public void GetEvents_ShouldReturnPaginatedResultWithEventsFilteredByName()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filter = new EventsFilter() { title = "встреча" };
            var fakeEvents = new List<Event>
            {
                Event.Create("Деловая всТреча", DateTime.Now, DateTime.Now.AddHours(1)),
                Event.Create("Ужин при свечах", DateTime.Now, DateTime.Now.AddHours(2)),
                Event.Create("встречА на высшем уровне", DateTime.Now, DateTime.Now.AddHours(3)),
            }.AsQueryable();

            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Act
            var result = service.GetEvents(filter);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(2); // Проверяем количество в текущей выборке
            repositoryMock.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public void GetEvents_ShouldReturnPaginatedResultWithEventsFilteredByStartDate_ReturnEqualOrBefore()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filter = new EventsFilter() { from = DateTime.Now.AddHours(2) };
            var fakeEvents = new List<Event>
            {
                Event.Create("Встреча 1", DateTime.Now.AddHours(1), DateTime.Now.AddHours(5)),
                Event.Create("Ужин при свечах", DateTime.Now.AddHours(3), DateTime.Now.AddHours(5)),
                Event.Create("Встреча 2", DateTime.Now.AddHours(1), DateTime.Now.AddHours(5)),
            }.AsQueryable();

            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Act
            var result = service.GetEvents(filter);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(2); // Проверяем количество в текущей выборке
            repositoryMock.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public void GetEvents_ShouldReturnPaginatedResultWithEventsFilteredByEndDate_ReturnEqualOrGreater()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filter = new EventsFilter() { to = DateTime.Now.AddHours(2) };
            var fakeEvents = new List<Event>
            {
                Event.Create("Встреча 1", DateTime.Now.AddHours(1), DateTime.Now.AddHours(1)),
                Event.Create("Ужин при свечах", DateTime.Now.AddHours(3), DateTime.Now.AddHours(12)),
                Event.Create("Встреча 2", DateTime.Now.AddHours(1), DateTime.Now.AddHours(1)),
            }.AsQueryable();

            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Act
            var result = service.GetEvents(filter);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(1); // Проверяем количество в текущей выборке
            repositoryMock.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public void GetEvents_ShouldReturnPaginatedResultWithSecondPageWithCountThree()
        {
            // Arrange (Подготовка)
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filter = new EventsFilter { page = 2, pageSize = 3 };

            // Создаем список тестовых данных, которые "якобы" есть в репозитории
            var fakeEvents = new List<Event>
            {
                Event.Create("Событие 1", DateTime.Now, DateTime.Now.AddHours(1)),
                Event.Create("Событие 2", DateTime.Now, DateTime.Now.AddHours(1)),
                Event.Create("Событие 3", DateTime.Now, DateTime.Now.AddHours(1)),
                Event.Create("Событие 4", DateTime.Now, DateTime.Now.AddHours(1)),
                Event.Create("Событие 5", DateTime.Now, DateTime.Now.AddHours(1)),
                Event.Create("Событие 6", DateTime.Now, DateTime.Now.AddHours(1))
            }.AsQueryable();

            // Настраиваем Mock репозитория возвращать этот список
            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Act (Действие)
            var result = service.GetEvents(filter);

            // Assert (Проверка)
            result.Should().NotBeNull();
            result.EventsTotalCount.Should().Be(6); // Проверяем общее количество
            result.Events.Should().HaveCount(3); // Проверяем количество в текущей выборке
            result.Events.Should().ContainSingle(e => e.Title == "Событие 5");
            result.Events.Should().BeInAscendingOrder(e => e.Title); // Проверяем сортировку, которая есть в сервисе

            repositoryMock.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public void GetEvents_ShouldReturnPaginatedResultWithOneEventWithManyFilters()
        {
            // Arrange (Подготовка)
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var targetDate = DateTime.Now.AddHours(3);

            // Создаем список тестовых данных, которые "якобы" есть в репозитории
            var fakeEvents = new List<Event>
            {
                Event.Create("Событие 1", targetDate, DateTime.Now.AddHours(5)),
                Event.Create("Неважная встреча", DateTime.Now.AddHours(1), DateTime.Now.AddHours(5)),
                Event.Create("Событие 3", DateTime.Now, DateTime.Now.AddHours(5)),
                Event.Create("Поразить цель с 10 шагов", targetDate, DateTime.Now.AddHours(5)),
                Event.Create("Поужинать ", DateTime.Now.AddHours(2), DateTime.Now.AddHours(5)),
                Event.Create("Событие 6", DateTime.Now.AddHours(1), DateTime.Now.AddHours(5))
            }.AsQueryable();

            // Настраиваем Mock репозитория возвращать этот список
            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Фильтр: ищем "10" и дату начала <= 3 часа от текущей
            var filter = new EventsFilter
            {
                title = "цель",
                from = targetDate,
                page = 1,
                pageSize = 10
            };


            // Act (Действие)
            var result = service.GetEvents(filter);

            // Assert (Проверка)
            result.Should().NotBeNull();
            result.EventsTotalCount.Should().Be(1); // Должно найтись только одно
            result.Events.Should().ContainSingle();
            result.Events.First().Title.Should().Contain("цель");
            result.Events.First().StartAt.Should().BeOnOrBefore(targetDate);

            repositoryMock.Verify(r => r.GetAll(), Times.Once);

        }

        #endregion

        #region GetEvent tests
        [Fact]
        [Trait("Category", "GetEvent")]
        public void GetEvent_ShouldReturnResponseEventDTO_IfEventExist()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var domainEvent = Event.Create("Тестовое событие 1", DateTime.Now, DateTime.Now.AddHours(2));
            var generatedId = domainEvent.Id;
            repositoryMock.Setup(r => r.GetById(generatedId)).Returns(domainEvent);

            // Act
            var result = service.GetEvent(generatedId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(domainEvent, options => options
                .Including(x => x.Title)
                .Including(x => x.StartAt)
                .Including(x => x.EndAt)
                .Including(x => x.Id));

            repositoryMock.Verify(r => r.GetById(generatedId), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvent")]
        public void GetEvent_ShouldThrowNotFoundException_IfEventDoesNotExist()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var generatedId = Guid.NewGuid();
            repositoryMock.Setup(r => r.GetById(generatedId)).Returns((Event?)null);

            // Act
            Action act = () => service.GetEvent(generatedId);

            // Assert
            act.Should().Throw<NotFoundException>();

            repositoryMock.Verify(r => r.GetById(generatedId), Times.Once);
        }

        #endregion

        #region ChangeEvent tests

        [Fact]
        [Trait("Category", "ChangeEvent")]
        public void ChangeEvent_ShouldThrowNotFoundException_IfEventDoesNotExist()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var nonExistentId = Guid.NewGuid();
            var updateDto = new UpdateEventDTO
            {
                Title = "Тестовое событие 1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
            };
            repositoryMock.Setup(r => r.GetById(nonExistentId)).Returns((Event?)null);

            // Act
            Action act = () => service.ChangeEvent(nonExistentId, updateDto);

            // Assert
            act.Should().Throw<NotFoundException>();

            // Проверяем, что метод Update у репозитория НИКОГДА не вызывался
            repositoryMock.Verify(r => r.Update(It.IsAny<Event>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "ChangeEvent")]
        public void ChangeEvent_ShouldThrowValidationCustomException_IfUpdateEventDTOAreInvalid()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);

            // 1. Создаем существующее событие (сначала валидное)
            var existedEvent = Event.Create("Тестовое событие 1", DateTime.Now, DateTime.Now.AddHours(1));
            var eventId = existedEvent.Id;

            // 2. Подготавливаем НЕВАЛИДНЫЕ данные для обновления (конец раньше начала)
            var invalidUpdateDto = new UpdateEventDTO
            {
                Title = "Тестовое событие 2",
                StartAt = DateTime.Now.AddHours(5),
                EndAt = DateTime.Now.AddHours(2), // ОШИБКА: Конец < Начало
            };

            // 3. Настраиваем Mock: репозиторий находит старое событие
            repositoryMock.Setup(r => r.GetById(eventId)).Returns(existedEvent);

            // Act
            // Оборачиваем вызов в Action для перехвата исключения
            Action act = () => service.ChangeEvent(eventId, invalidUpdateDto);

            // Assert
            // Проверяем, что выброшено нужное исключение с правильным сообщением
            act.Should().Throw<ValidationCustomException>();

            // Проверяем, что репозиторий НЕ вызывал метод Update, так как валидация провалилась
            repositoryMock.Verify(r => r.Update(It.IsAny<Event>()), Times.Never);
        }


        [Fact]
        [Trait("Category", "ChangeEvent")]
        public void ChangeEvent_ShouldChangeAllEventsData()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            // 1. Создаем существующее событие в "базе"
            var existedEvent = Event.Create("Старое название", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Старое описание");
            var eventId = existedEvent.Id;

            // 2. Данные для обновления
            var updateDto = new UpdateEventDTO
            {
                Title = "Новое название",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(2),
                Description = "Новое описание"
            };

            // 3. Настраиваем Mock репозитория
            repositoryMock.Setup(r => r.GetById(eventId)).Returns(existedEvent);

            // Act
            service.ChangeEvent(eventId, updateDto);

            // Assert
            // Проверяем, что поля объекта действительно изменились на данные из DTO

            existedEvent.Should().BeEquivalentTo(updateDto, options => options
                .Including(x => x.Title)
                .Including(x => x.StartAt)
                .Including(x => x.EndAt)
            );

            // Проверяем, что сервис вызвал Update у репозитория ровно один раз с этим объектом
            repositoryMock.Verify(r => r.Update(existedEvent), Times.Once);
        }

        #endregion

        #region CancelEvent tests

        [Fact]
        public void CancelEvent_ShouldDeleteEvent_WhenEventExists()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            repositoryMock.Setup(r => r.Delete(eventId)).Returns(true);

            // Act
            service.CancelEvent(eventId);

            // Assert
            repositoryMock.Verify(r => r.Delete(eventId), Times.Once);
        }

        [Fact]
        public void CancelEvent_ShouldThrowNotFoundException_WhenEventDoesNotExist()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            repositoryMock.Setup(r => r.Delete(eventId)).Returns(false);

            // Act
            Action act = () => service.CancelEvent(eventId);

            // Assert
            act.Should().Throw<NotFoundException>();
        }

        #endregion
    }
}
