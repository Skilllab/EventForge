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
        public async Task CreateEvent_ShouldReturnResponseEventDTO_WhenCreateEventDTOIsValid()
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
            var result = await service.CreateEventAsync(dto, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(dto, options => options
                .Including(x => x.Title)
                .Including(x => x.StartAt)
                .Including(x => x.EndAt));

            // Проверяем, что репозиторий действительно вызывался один раз
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        [Trait("Category", "CreateEvent")]
        public async Task CreateEvent_ShouldThrowValidationException_WhenCreateEventDTOAreInvalid()
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
            Func<Task> act = async () => await service.CreateEventAsync(dto, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationCustomException>();

            // Проверяем, что метод добавления в репозиторий не вызывался, так как данные невалидные
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), CancellationToken.None), Times.Never);
        }

        #endregion

        #region GetEvents tests

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldReturnPaginatedResultWithAllEvents()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filter = new EventsFilter();
            var now = DateTime.Now;
            var fakeEvents = new List<Event>
            {
                Event.Create("тестовое событие 1", now, now.AddHours(1)),
                Event.Create("тестовое событие 34", now, now.AddHours(2)),
                Event.Create("тестовое событие 2", now, now.AddHours(3)),
            }.AsQueryable();

            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Act
            var result = await service.GetEventsAsync(filter, CancellationToken.None);

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
        public async Task GetEvents_ShouldReturnPaginatedResultWithEventsFilteredByName()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filteredWord = "встреча";
            var filter = new EventsFilter() { title = filteredWord };
            var now = DateTime.Now;
            var fakeEvents = new List<Event>
            {
                Event.Create("Деловая всТреча", now, now.AddHours(1)),
                Event.Create("Ужин при свечах", now, now.AddHours(2)),
                Event.Create("встречА на высшем уровне", now, now.AddHours(3)),
            }.AsQueryable();

            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Act
            var result = await service.GetEventsAsync(filter, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(2); // Проверяем количество в текущей выборке
            result.Events.Should().OnlyContain(t => t.Title.Contains(filteredWord, StringComparison.CurrentCultureIgnoreCase));
            repositoryMock.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldReturnPaginatedResultWithEventsFilteredByStartDate_ReturnEqualOrBefore()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;
            var filter = new EventsFilter() { from = now.AddHours(2) };
            var fakeEvents = new List<Event>
            {
                Event.Create("Встреча 1", now.AddHours(1), now.AddHours(5)),
                Event.Create("Встреча 2", now.AddHours(2), now.AddHours(5)),
                Event.Create("Ужин при свечах", now.AddHours(3), now.AddHours(5))
            }.AsQueryable();

            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Act
            var result = await service.GetEventsAsync(filter, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(2); // Проверяем количество в текущей выборке
            repositoryMock.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldReturnPaginatedResultWithEventsFilteredByEndDate_ReturnEqualOrGreater()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;
            var filter = new EventsFilter() { to = now.AddHours(2) };

            var fakeEvents = new List<Event>
            {
                Event.Create("Встреча 1", now.AddHours(1), now.AddHours(1)),
                Event.Create("Ужин при свечах", now.AddHours(3), now.AddHours(12)),
                Event.Create("Встреча 2", now.AddHours(1), now.AddHours(1)),
            }.AsQueryable();

            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Act
            var result = await service.GetEventsAsync(filter, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(2); // Проверяем количество в текущей выборке
            repositoryMock.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldReturnPaginatedResultWithSecondPageWithCountThree()
        {
            // Arrange (Подготовка)
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var filter = new EventsFilter { page = 2, pageSize = 3 };
            var now = DateTime.Now;
            // Создаем список тестовых данных, которые "якобы" есть в репозитории
            var fakeEvents = new List<Event>
            {
                Event.Create("Событие 1", now, now.AddHours(1)),
                Event.Create("Событие 2", now, now.AddHours(1)),
                Event.Create("Событие 3", now, now.AddHours(1)),
                Event.Create("Событие 4", now, now.AddHours(1)),
                Event.Create("Событие 5", now, now.AddHours(1)),
                Event.Create("Событие 6", now, now.AddHours(1))
            }.AsQueryable();

            // Настраиваем Mock репозитория возвращать этот список
            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // Act (Действие)
            var result = await service.GetEventsAsync(filter, CancellationToken.None);

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
        public async Task GetEvents_ShouldReturnPaginatedResultWithOneEventWithManyFilters()
        {
            // Arrange (Подготовка)
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var targetDate = DateTime.Now.AddHours(3);
            var now = DateTime.Now;
            // Создаем список тестовых данных, которые "якобы" есть в репозитории
            var fakeEvents = new List<Event>
            {
                Event.Create("Событие 1", targetDate, now.AddHours(5)),
                Event.Create("Неважная встреча", now.AddHours(1), now.AddHours(5)),
                Event.Create("Событие 3", now, now.AddHours(5)),
                Event.Create("Поразить цель с 10 шагов", targetDate, now.AddHours(5)),
                Event.Create("Поужинать ", now.AddHours(2), now.AddHours(5)),
                Event.Create("Событие 6", now.AddHours(1), now.AddHours(5))
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
            var result = await service.GetEventsAsync(filter, CancellationToken.None);

            // Assert (Проверка)
            result.Should().NotBeNull();
            result.EventsTotalCount.Should().Be(1); // Должно найтись только одно
            result.Events.Should().ContainSingle();
            result.Events.First().Title.Should().Contain("цель");
            result.Events.First().StartAt.Should().BeOnOrBefore(targetDate);

            repositoryMock.Verify(r => r.GetAll(), Times.Once);

        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldApplyPagination_CorrectSkipAndTake()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;

            //События добавлены в отсортированном порядке
            var fakeEvents = new List<Event>
            {
                Event.Create("Неважная встреча",
                    now.AddHours(1), now.AddHours(5)),

                Event.Create("Поразить цель с 10 шагов",
                    now.AddHours(3), now.AddHours(5)),

                Event.Create("Поужинать ",
                    now.AddHours(2), now.AddHours(5)),

                Event.Create("Событие 1",
                    now.AddHours(3), now.AddHours(5)),

                Event.Create("Событие 3",
                    now, now.AddHours(5)),

                Event.Create("Событие 6",
                    now.AddHours(1), now.AddHours(5))

            }.AsQueryable();

            repositoryMock.Setup(r => r.GetAll()).Returns(fakeEvents);

            // 2-я страница по 2 элемента
            var filter = new EventsFilter { page = 2, pageSize = 2 };

            // Act
            var result = await service.GetEventsAsync(filter, CancellationToken.None);

            // Assert
            result.EventsTotalCount.Should().Be(6); // Общее количество не меняется
            result.Events.Should().HaveCount(2);   // На странице только 2

            result.Events.Should().Contain(e => e.Title.Equals("Поужинать ", StringComparison.CurrentCultureIgnoreCase));
            result.Events.Should().Contain(e => e.Title.Equals("Событие 1", StringComparison.CurrentCultureIgnoreCase));
        }

        #endregion

        #region GetEvent tests
        [Fact]
        [Trait("Category", "GetEvent")]
        public async Task GetEvent_ShouldReturnResponseEventDTO_IfEventExist()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var domainEvent = Event.Create("Тестовое событие 1", DateTime.Now, DateTime.Now.AddHours(2));
            var generatedId = domainEvent.Id;
            repositoryMock.Setup(r => r.GetByIdAsync(generatedId, CancellationToken.None)).ReturnsAsync(domainEvent);

            // Act
            var result = await service.GetEventAsync(generatedId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(domainEvent, options => options
                .Including(x => x.Title)
                .Including(x => x.StartAt)
                .Including(x => x.EndAt)
                .Including(x => x.Id));

            repositoryMock.Verify(r => r.GetByIdAsync(generatedId, CancellationToken.None), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvent")]
        public async Task GetEvent_ShouldThrowNotFoundException_IfEventDoesNotExist()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var generatedId = Guid.NewGuid();
            repositoryMock.Setup(r => r.GetByIdAsync(generatedId, CancellationToken.None)).ReturnsAsync((Event?) null);

            // Act
            Func<Task> act = async () => await service.GetEventAsync(generatedId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            repositoryMock.Verify(r => r.GetByIdAsync(generatedId, CancellationToken.None), Times.Once);
        }

        #endregion

        #region ChangeEvent tests

        [Fact]
        [Trait("Category", "ChangeEvent")]
        public async Task ChangeEvent_ShouldThrowNotFoundException_IfEventDoesNotExist()
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
            repositoryMock.Setup(r => r.GetByIdAsync(nonExistentId, CancellationToken.None)).ReturnsAsync((Event?) null);

            // Act
            Func<Task> act = async () => await service.ChangeEventAsync(nonExistentId, updateDto, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            // Проверяем, что метод Update у репозитория НИКОГДА не вызывался
            repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), CancellationToken.None), Times.Never);
        }

        [Fact]
        [Trait("Category", "ChangeEvent")]
        public async Task ChangeEvent_ShouldThrowValidationCustomException_IfUpdateEventDTOAreInvalid()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;
            var existedEvent = Event.Create("Старое событие", now, now.AddHours(1));
            var eventId = existedEvent.Id;

            // Подготавливаем НЕВАЛИДНЫЕ данные: Начало (5ч) > Конец (2ч)
            var invalidUpdateDto = new UpdateEventDTO
            {
                Title = "Обновление",
                StartAt = now.AddHours(5),
                EndAt = now.AddHours(2)
            };

            repositoryMock.Setup(r => r.GetByIdAsync(eventId, CancellationToken.None)).ReturnsAsync(existedEvent);

            // Act
            Func<Task> act = async () => await service.ChangeEventAsync(eventId, invalidUpdateDto, CancellationToken.None);

            // Assert
            // Проверяем выброс исключения (логика внутри ChangeEvent должна проверять .Value у Nullable дат)
           await act.Should().ThrowAsync<ValidationCustomException>();

            repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), CancellationToken.None), Times.Never);
        }


        [Fact]
        [Trait("Category", "ChangeEvent")]
        public async Task ChangeEvent_ShouldChangeAllEventsData()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            var now = DateTime.Now;

            var existedEvent = Event.Create("Старое название", now, now.AddHours(1), "Старое описание");
            var eventId = existedEvent.Id;

            var updateDto = new UpdateEventDTO
            {
                Title = "Новое название",
                StartAt = now.AddDays(1),
                EndAt = now.AddDays(1).AddHours(2),
                Description = "Новое описание"
            };

            repositoryMock.Setup(r => r.GetByIdAsync(eventId, CancellationToken.None)).ReturnsAsync(existedEvent);

            // Act
            await service.ChangeEventAsync(eventId, updateDto, CancellationToken.None);

            // Assert
            existedEvent.Should().BeEquivalentTo(updateDto, options => options
                .Including(x => x.Title)
                .Including(x => x.StartAt)
                .Including(x => x.EndAt)
            );

            // Проверяем, что сервис вызвал Update у репозитория ровно один раз с этим объектом
            repositoryMock.Verify(r => r.UpdateAsync(existedEvent, CancellationToken.None), Times.Once);
        }

        #endregion

        #region CancelEvent tests

        [Fact]
        public async Task CancelEvent_ShouldDeleteEvent_WhenEventExists()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            repositoryMock.Setup(r => r.DeleteAsync(eventId, CancellationToken.None)).ReturnsAsync(true);

            // Act
            await service.CancelEventAsync(eventId, CancellationToken.None);

            // Assert
            repositoryMock.Verify(r => r.DeleteAsync(eventId, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task CancelEvent_ShouldThrowNotFoundException_WhenEventDoesNotExist()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var service = new EventService(repositoryMock.Object, loggerMock.Object);
            repositoryMock.Setup(r => r.DeleteAsync(eventId, CancellationToken.None)).ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await service.CancelEventAsync(eventId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        #endregion
    }
}
