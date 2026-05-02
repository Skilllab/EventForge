using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;
using EventBookingService.Domain.Interfaces;
using EventBookingService.WebAPI.Application.Services;
using EventBookingService.WebAPI.Models.DTO.Events;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

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
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var ct = CancellationToken.None;
            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие",
                StartAt = now,
                EndAt = now.AddHours(1),
                TotalSeats = 1
            };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

            // Act
            var result = await service.CreateEventAsync(dto, ct);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(dto, options => options
                .Including(x => x.Title)
                .Including(x => x.StartAt)
                .Including(x => x.EndAt));

            // Проверяем, что репозиторий действительно вызывался один раз
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "CreateEvent")]
        public async Task CreateEvent_ShouldThrowValidationException_WhenCreateEventDTOAreInvalidByDate()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var ct = CancellationToken.None;
            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие с невалидной моделью данных",
                StartAt = now.AddHours(2),
                EndAt = now.AddHours(1)
            };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

            // Act
            Func<Task> act = async () => await service.CreateEventAsync(dto, ct);

            // Assert
            await act.Should().ThrowAsync<ValidationCustomException>();

            // Проверяем, что метод добавления в репозиторий не вызывался, так как данные невалидные
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), ct), Times.Never);
        }

        [Fact]
        [Trait("Category", "CreateEvent")]
        public async Task CreateEvent_ShouldThrowValidationException_WhenCreateEventDTOAreInvalidByZeroSeats()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var ct = CancellationToken.None;
            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие с невалидной моделью данных",
                StartAt = now,
                EndAt = now.AddHours(1),
                TotalSeats = 0

            };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

            // Act
            Func<Task> act = async () => await service.CreateEventAsync(dto, ct);

            // Assert
            await act.Should().ThrowAsync<ValidationCustomException>();

            // Проверяем, что метод добавления в репозиторий не вызывался, так как данные невалидные
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), ct), Times.Never);
        }

        [Fact]
        [Trait("Category", "CreateEvent")]
        public async Task CreateEvent_ShouldThrowIfCancelled()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var dto = new CreateEventDTO
            {
                Title = "Тестовое событие",
                StartAt = now.AddHours(1),
                EndAt = now.AddHours(2) // Конец позже начала
            };
            repositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));

            // Act
            Func<Task> act = async () => await service.CreateEventAsync(dto, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);

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
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var filter = new EventsFilter();
            var totalSeats = 1;
            var ct = CancellationToken.None;
            var fakeEvents = new List<Event>
            {
                Event.Create("тестовое событие 1", now, now.AddHours(1), totalSeats),
                Event.Create("тестовое событие 34", now, now.AddHours(2), totalSeats),
                Event.Create("тестовое событие 2", now, now.AddHours(3), totalSeats)
            };
            repositoryMock.Setup(r => r.GetTotalCount(It.IsAny<CancellationToken>())).Returns(fakeEvents.Count);

            repositoryMock.Setup(r => r.GetAll(It.IsAny<Func<Event, bool>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(fakeEvents);

            // Act
            var result = await service.GetEventsAsync(filter, ct);

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

            repositoryMock.Verify(r => r.GetAll(It.IsAny<Func<Event, bool>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldReturnPaginatedResultWithEventsFilteredByName()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var filteredWord = "встреча";
            var ct = CancellationToken.None;
            var totalSeats = 1;
            var filter = new EventsFilter() { title = filteredWord };
            var fakeEvents = new List<Event>
            {
                Event.Create("Деловая всТреча", now, now.AddHours(1), totalSeats),
                Event.Create("Ужин при свечах", now, now.AddHours(2), totalSeats),
                Event.Create("встречА на высшем уровне", now, now.AddHours(3), totalSeats)
            };

            repositoryMock
                .Setup(r => r.GetAll(
                    It.IsAny<Func<Event, bool>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns((Func<Event, bool> query, int _, int _, CancellationToken _) => fakeEvents.Where(query).ToList());

            // Act
            var result = await service.GetEventsAsync(filter, ct);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(2); // Проверяем количество в текущей выборке
            result.Events.Should().OnlyContain(t => t.Title.Contains(filteredWord, StringComparison.CurrentCultureIgnoreCase));
            repositoryMock.Verify(r => r.GetAll(It.IsAny<Func<Event, bool>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldReturnPaginatedResultWithEventsFilteredByStartDate_ReturnEqualOrBefore()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var totalSeats = 1;
            var filter = new EventsFilter() { from = now.AddHours(2) };
            var ct = CancellationToken.None;
            var fakeEvents = new List<Event>
            {
                Event.Create("Встреча 1", now.AddHours(1), now.AddHours(5), totalSeats),
                Event.Create("Встреча 2", now.AddHours(2), now.AddHours(5), totalSeats),
                Event.Create("Ужин при свечах", now.AddHours(3), now.AddHours(5), totalSeats)
            };

            repositoryMock
                .Setup(r => r.GetAll(
                    It.IsAny<Func<Event, bool>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns((Func<Event, bool> query, int _, int _, CancellationToken _) => fakeEvents.Where(query).ToList());

            // Act
            var result = await service.GetEventsAsync(filter, ct);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(2); // Проверяем количество в текущей выборке
            repositoryMock.Verify(r => r.GetAll(It.IsAny<Func<Event, bool>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldReturnPaginatedResultWithEventsFilteredByEndDate_ReturnEqualOrGreater()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var totalSeats = 1;
            var filter = new EventsFilter() { to = now.AddHours(1) };
            var ct = CancellationToken.None;
            var fakeEvents = new List<Event>
            {
                Event.Create("Встреча 1", now.AddHours(1), now.AddHours(1), totalSeats),
                Event.Create("Ужин при свечах", now.AddHours(3), now.AddHours(12), totalSeats),
                Event.Create("Встреча 2", now.AddHours(1), now.AddHours(1), totalSeats)
            };

            repositoryMock
                .Setup(r => r.GetAll(
                    It.IsAny<Func<Event, bool>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns((Func<Event, bool> query, int _, int _, CancellationToken _) => fakeEvents.Where(query).ToList());

            // Act
            var result = await service.GetEventsAsync(filter, ct);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(2); // Проверяем количество в текущей выборке
            repositoryMock.Verify(r => r.GetAll(It.IsAny<Func<Event, bool>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldReturnPaginatedResultWithEventsFilteredByEndDate_IfEndDateWithoutTime_ReturnEqualOrGreater()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var totalSeats = 1;
            var filter = new EventsFilter() { to = now.Date };
            var ct = CancellationToken.None;
            var fakeEvents = new List<Event>
            {
                Event.Create("Встреча 1", now.AddHours(1), now.AddHours(1), totalSeats),
                Event.Create("Ужин при свечах", now.AddHours(3), now.AddHours(12), totalSeats),
                Event.Create("Встреча 2", now.AddHours(1), now.AddHours(1), totalSeats),
                Event.Create("Встреча 22", now.AddHours(1), now.AddHours(22), totalSeats)
            };

            repositoryMock
                .Setup(r => r.GetAll(
                    It.IsAny<Func<Event, bool>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns((Func<Event, bool> query, int _, int _, CancellationToken _) => fakeEvents.Where(query).ToList());

            // Act
            var result = await service.GetEventsAsync(filter, ct);

            // Assert
            result.Should().NotBeNull();
            result.Events.Should().HaveCount(2); // Проверяем количество в текущей выборке
            repositoryMock.Verify(r => r.GetAll(It.IsAny<Func<Event, bool>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldReturnPaginatedResultWithSecondPageWithCountThree()
        {
            // Arrange (Подготовка)
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var filter = new EventsFilter { page = 2, pageSize = 3 };
            var totalSeats = 1;
            var ct = CancellationToken.None;
            // Создаем список тестовых данных, которые "якобы" есть в репозитории
            var fakeEvents = new List<Event>
            {
                Event.Create("Событие 1", now, now.AddHours(1), totalSeats),
                Event.Create("Событие 2", now, now.AddHours(1), totalSeats),
                Event.Create("Событие 3", now, now.AddHours(1), totalSeats),
                Event.Create("Событие 4", now, now.AddHours(1), totalSeats),
                Event.Create("Событие 5", now, now.AddHours(1), totalSeats),
                Event.Create("Событие 6", now, now.AddHours(1), totalSeats)
            };

            // Настраиваем Mock репозитория возвращать этот список
            repositoryMock
                .Setup(r => r.GetAll(
                    It.IsAny<Func<Event, bool>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns((Func<Event, bool> query, int page, int pageSize, CancellationToken _) =>
                {
                    return fakeEvents
                        .Where(query)
                        .OrderBy(e => e.Title) // Важно для консистентности страниц
                        .Skip((page - 1) * pageSize) // Пропускаем предыдущие страницы
                        .Take(pageSize)              // Берем только нужные элементы
                        .ToList();
                });

            repositoryMock.Setup(r => r.GetTotalCount(It.IsAny<CancellationToken>())).Returns(fakeEvents.Count);

            // Act (Действие)
            var result = await service.GetEventsAsync(filter, ct);

            // Assert (Проверка)
            result.Should().NotBeNull();
            result.EventsTotalCount.Should().Be(6); // Проверяем общее количество
            result.Events.Should().HaveCount(3); // Проверяем количество в текущей выборке
            result.Events.Should().ContainSingle(e => e.Title == "Событие 5");
            result.Events.Should().BeInAscendingOrder(e => e.Title); // Проверяем сортировку, которая есть в сервисе

            repositoryMock.Verify(r => r.GetAll(It.IsAny<Func<Event, bool>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldReturnPaginatedResultWithOneEventWithManyFilters()
        {
            // Arrange (Подготовка)
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var targetDate = now.AddHours(3);
            var totalSeats = 1;
            var ct = CancellationToken.None;
            // Создаем список тестовых данных, которые "якобы" есть в репозитории
            var fakeEvents = new List<Event>
            {
                Event.Create("Событие 1", targetDate, now.AddHours(5), totalSeats),
                Event.Create("Неважная встреча", now.AddHours(1), now.AddHours(5), totalSeats),
                Event.Create("Событие 3", now, now.AddHours(5), totalSeats),
                Event.Create("Поразить цель с 10 шагов", targetDate, now.AddHours(5), totalSeats),
                Event.Create("Поужинать ", now.AddHours(2), now.AddHours(5), totalSeats),
                Event.Create("Событие 6", now.AddHours(1), now.AddHours(5), totalSeats)
            };

            // Настраиваем Mock репозитория возвращать этот список
            repositoryMock
                .Setup(r => r.GetAll(
                    It.IsAny<Func<Event, bool>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns((Func<Event, bool> query, int _, int _, CancellationToken _) => fakeEvents.Where(query).ToList());
            repositoryMock.Setup(r => r.GetTotalCount(It.IsAny<CancellationToken>())).Returns(fakeEvents.Count);


            // Фильтр: ищем "10" и дату начала <= 3 часа от текущей
            var filter = new EventsFilter
            {
                title = "цель",
                from = targetDate,
                page = 1,
                pageSize = 10
            };

            // Act (Действие)
            var result = await service.GetEventsAsync(filter, ct);

            // Assert (Проверка)
            result.Should().NotBeNull();
            result.EventsTotalCount.Should().Be(6); 
            result.Events.Should().ContainSingle();
            result.Events.First().Title.Should().Contain("цель");
            result.Events.First().StartAt.Should().BeOnOrBefore(targetDate);

            repositoryMock.Verify(r => r.GetAll(It.IsAny<Func<Event, bool>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldApplyPagination_CorrectSkipAndTake()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var totalSeats = 1;
            var ct = CancellationToken.None;

            //События добавлены в отсортированном порядке
            var fakeEvents = new List<Event>
            {
                Event.Create("Неважная встреча", now.AddHours(1), now.AddHours(5), totalSeats),

                Event.Create("Поразить цель с 10 шагов", now.AddHours(3), now.AddHours(5), totalSeats),

                Event.Create("Поужинать ", now.AddHours(2), now.AddHours(5), totalSeats),

                Event.Create("Событие 1", now.AddHours(3), now.AddHours(5), totalSeats),

                Event.Create("Событие 3", now, now.AddHours(5), totalSeats),

                Event.Create("Событие 6", now.AddHours(1), now.AddHours(5), totalSeats)

            };
            repositoryMock
                .Setup(r => r.GetTotalCount(It.IsAny<CancellationToken>()))
                .Returns(fakeEvents.Count);

            var filter = new EventsFilter { page = 2, pageSize = 2 };
            var pagedData = fakeEvents.Skip(2).Take(2).ToList();

            repositoryMock
                .Setup(r => r.GetAll(It.IsAny<Func<Event, bool>>(), filter.page, filter.pageSize, It.IsAny<CancellationToken>()))
                .Returns(pagedData);

            // Act
            var result = await service.GetEventsAsync(filter, ct);

            // Assert
            result.EventsTotalCount.Should().Be(6); // Общее количество не меняется
            result.Events.Should().HaveCount(2);   // На странице только 2
            result.Events.Should().Contain(e => e.Title.Equals("Поужинать ", StringComparison.CurrentCultureIgnoreCase));
            result.Events.Should().Contain(e => e.Title.Equals("Событие 1", StringComparison.CurrentCultureIgnoreCase));

            repositoryMock.Verify(r => r.GetAll(It.IsAny<Func<Event, bool>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

        }

        [Fact]
        [Trait("Category", "GetEvents")]
        public async Task GetEvents_ShouldThrowIfCancelled()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var totalSeats = 1;
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var fakeEvents = new List<Event>
            {
                Event.Create("Событие 1", now.AddHours(1), now.AddHours(2), totalSeats)
            };
            var filter = new EventsFilter { page = 2, pageSize = 2 };
            repositoryMock.Setup(r => r.GetAll(It.IsAny<Func<Event, bool>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(fakeEvents);

            // Act
            Func<Task> act = async () => await service.GetEventsAsync(filter, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
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
            var totalSeats = 1;
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var domainEvent = Event.Create("Тестовое событие 1", now, now.AddHours(2), totalSeats);
            var generatedId = domainEvent.Id;
            var ct = CancellationToken.None;
            repositoryMock.Setup(r => r.GetByIdAsync(generatedId, It.IsAny<CancellationToken>())).ReturnsAsync(domainEvent);

            // Act
            var result = await service.GetEventAsync(generatedId, ct);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(domainEvent, options => options
                .Including(x => x.Title)
                .Including(x => x.StartAt)
                .Including(x => x.EndAt)
                .Including(x => x.Id));

            repositoryMock.Verify(r => r.GetByIdAsync(generatedId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvent")]
        public async Task GetEvent_ShouldThrowNotFoundException_IfEventDoesNotExist()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var generatedId = Guid.NewGuid();
            var ct = CancellationToken.None;

            repositoryMock.Setup(r => r.GetByIdAsync(generatedId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?) null);

            // Act
            Func<Task> act = async () => await service.GetEventAsync(generatedId, ct);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            repositoryMock.Verify(r => r.GetByIdAsync(generatedId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "GetEvent")]
        public async Task GetEvent_ShouldThrowIfCancelled()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(It.IsAny<Event>());

            // Act
            Func<Task> act = async () => await service.GetEventAsync(It.IsAny<Guid>(), cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
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
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var nonExistentId = Guid.NewGuid();
            var ct = CancellationToken.None;

            var updateDto = new UpdateEventDTO
            {
                Title = "Тестовое событие 1",
                StartAt = now,
                EndAt = now.AddHours(1),
            };
            repositoryMock.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?) null);

            // Act
            Func<Task> act = async () => await service.ChangeEventAsync(nonExistentId, updateDto, ct);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            // Проверяем, что метод Update у репозитория НИКОГДА не вызывался
            repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "ChangeEvent")]
        public async Task ChangeEvent_ShouldThrowValidationCustomException_IfUpdateEventDTOAreInvalid()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var totalSeats = 1;
            var existedEvent = Event.Create("Старое событие", now, now.AddHours(1), totalSeats);
            var eventId = existedEvent.Id;
            var ct = CancellationToken.None;


            // Подготавливаем НЕВАЛИДНЫЕ данные: Начало (5ч) > Конец (2ч)
            var invalidUpdateDto = new UpdateEventDTO
            {
                Title = "Обновление",
                StartAt = now.AddHours(5),
                EndAt = now.AddHours(2)
            };

            repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(existedEvent);

            // Act
            Func<Task> act = async () => await service.ChangeEventAsync(eventId, invalidUpdateDto, ct);

            // Assert
            // Проверяем выброс исключения (логика внутри ChangeEvent должна проверять .Value у Nullable дат)
           await act.Should().ThrowAsync<ValidationCustomException>();

            repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        [Fact]
        [Trait("Category", "ChangeEvent")]
        public async Task ChangeEvent_ShouldChangeAllEventsData()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var totalSeats = 1;
            var ct = CancellationToken.None;

            var existedEvent = Event.Create("Старое название", now, now.AddHours(1), totalSeats, "Старое описание");
            var eventId = existedEvent.Id;

            var updateDto = new UpdateEventDTO
            {
                Title = "Новое название",
                StartAt = now.AddDays(1),
                EndAt = now.AddDays(1).AddHours(2),
                Description = "Новое описание"
            };

            repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(existedEvent);

            // Act
            await service.ChangeEventAsync(eventId, updateDto, ct);

            // Assert
            existedEvent.Should().BeEquivalentTo(updateDto, options => options
                .Including(x => x.Title)
                .Including(x => x.StartAt)
                .Including(x => x.EndAt)
            );

            // Проверяем, что сервис вызвал Update у репозитория ровно один раз с этим объектом
            repositoryMock.Verify(r => r.UpdateAsync(existedEvent, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "ChangeEvent")]
        public async Task ChangeEvent_ShouldThrowIfCancelled()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(It.IsAny<Event>());

            // Act
            Func<Task> act = async () => await service.ChangeEventAsync(It.IsAny<Guid>(), It.IsAny<UpdateEventDTO>(), cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region CancelEvent tests

        [Fact]
        [Trait("Category", "CancelEvent")]
        public async Task CancelEvent_ShouldDeleteEvent_WhenEventExists()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var ct = CancellationToken.None;

            repositoryMock.Setup(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            await service.CancelEventAsync(eventId, ct);

            // Assert
            repositoryMock.Verify(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "CancelEvent")]
        public async Task CancelEvent_ShouldThrowNotFoundException_WhenEventDoesNotExist()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();

            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            var ct = CancellationToken.None;

            repositoryMock.Setup(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await service.CancelEventAsync(eventId, ct);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            repositoryMock.Verify(r => r.DeleteAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "CancelEvent")]
        public async Task CancelEvent_ShouldThrowIfCancelled()
        {
            // Arrange
            var repositoryMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<EventService>>();

            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);

            var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var id = Guid.NewGuid();
            repositoryMock.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await service.CancelEventAsync(id, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            repositoryMock.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion
    }
}
