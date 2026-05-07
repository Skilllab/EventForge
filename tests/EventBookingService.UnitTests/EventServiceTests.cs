using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;
using EventBookingService.Domain.Interfaces;
using EventBookingService.WebAPI.Application.Services;
using EventBookingService.WebAPI.Models.DTO.Events;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Moq;

namespace EventBookingService.Tests;

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
        var filter = new EventsFilter { page = 1, pageSize = 10 };
        var ct = CancellationToken.None;

        var fakeEvents = new List<Event>
        {
            Event.Create("test event 1", now, now.AddHours(1), 10),
            Event.Create("test event 2", now, now.AddHours(2), 10)
        };

        // Создаем record PagedResult: сначала Items, потом TotalCount
        var pagedResult = new PagedResult<Event>(fakeEvents, 2L);

        repositoryMock
            .Setup(r => r.GetPagedAsync(
                filter.title,
                filter.from,
                filter.to,
                filter.page,
                filter.pageSize,
                ct))
            .ReturnsAsync(pagedResult); // Теперь типы Task<PagedResult<Event>> совпадают идеально

        // Act
        var result = await service.GetEventsAsync(filter, ct);

        // Assert
        result.Should().NotBeNull();
        result.EventsTotalCount.Should().Be(2);
        result.CurrentPageNumber.Should().Be(filter.page);
        result.EventsCountOnCurrentPage.Should().Be(filter.pageSize);

        result.Events.Should().HaveCount(2);
        result.Events.Should().BeEquivalentTo(fakeEvents, options => options
            .Including(x => x.Title)
            .Including(x => x.StartAt)
            .Including(x => x.EndAt)
        );

        repositoryMock.Verify(r => r.GetPagedAsync(
                filter.title, filter.from, filter.to, filter.page, filter.pageSize, ct),
            Times.Once);
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
        var totalSeats = 1;
        var filter = new EventsFilter { title = filteredWord, page = 1, pageSize = 10 };
        var ct = CancellationToken.None;

        // События, которые прошли фильтр (например, 2 из 3)
        var fakeEvents = new List<Event>
        {
            Event.Create("Деловая всТреча", now, now.AddHours(1), totalSeats),
            Event.Create("Ужин при свечах", now, now.AddHours(2), totalSeats),
            Event.Create("встречА на высшем уровне", now, now.AddHours(3), totalSeats)
        };

        repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken token) =>
            {
                // Имитируем логику репозитория: Фильтруем список (имитация Where)
                var filtered = fakeEvents
                    .Where(e => string.IsNullOrEmpty(title) || e.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return new PagedResult<Event>(filtered, fakeEvents.Count);
            });


        // Act
        var result = await service.GetEventsAsync(filter, ct);

        // Assert
        result.Should().NotBeNull();

        result.Events.Should().HaveCount(2);
        result.Events.Should().OnlyContain(e => e.Title.Contains(filteredWord, StringComparison.OrdinalIgnoreCase));

        result.EventsTotalCount.Should().Be(3);

        result.CurrentPageNumber.Should().Be(1);
        result.EventsCountOnCurrentPage.Should().Be(10);

        repositoryMock.Verify(r => r.GetPagedAsync(filteredWord, null, null, 1, 10, ct), Times.Once);
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
            .Setup(r => r.GetPagedAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, DateTime? from, DateTime? _, int _, int _, CancellationToken _) =>
            {
                // Имитируем логику репозитория: фильтруем по дате начала
                var filtered = fakeEvents
                    .Where(e => !from.HasValue || e.StartAt >= from.Value)
                    .ToList();

                return new PagedResult<Event>(filtered, fakeEvents.Count);
            });

        // Act
        var result = await service.GetEventsAsync(filter, ct);

        // Assert
        result.Should().NotBeNull();

        result.Events.Should().HaveCount(2);
        result.Events.Should().OnlyContain(e => e.StartAt >= filter.from);
        result.EventsTotalCount.Should().Be(3);

        repositoryMock.Verify(r => r.GetPagedAsync(null, filter.from, null, 1, 10, ct), Times.Once);
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
        var filter = new EventsFilter { to = now.AddHours(1), page = 1, pageSize = 10 };
        var ct = CancellationToken.None;
        var totalSeats = 1;

        var allEventsInDb = new List<Event>
        {
            Event.Create("Встреча 1", now, now.AddHours(1), totalSeats),
            Event.Create("Ужин при свечах", now.AddHours(3), now.AddHours(12), totalSeats),
            Event.Create("Встреча 2", now, now.AddHours(1), totalSeats)
        };

        repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string t, DateTime? from, DateTime? to, int p, int ps, CancellationToken token) =>
            {
                // Имитируем логику репозитория для EndAt
                var filtered = allEventsInDb
                    .Where(e => !to.HasValue || (to.Value.TimeOfDay == TimeSpan.Zero
                        ? e.EndAt.Date <= to.Value.Date
                        : e.EndAt <= to.Value))
                    .ToList();

                return new PagedResult<Event>(filtered, allEventsInDb.Count);
            });

        // Act
        var result = await service.GetEventsAsync(filter, ct);

        // Assert
        result.Should().NotBeNull();
        result.Events.Should().HaveCount(2);
        result.Events.Should().AllSatisfy(e => e.EndAt.Should().BeOnOrBefore(filter.to.Value));
        result.EventsTotalCount.Should().Be(3);

        repositoryMock.Verify(r => r.GetPagedAsync(null, null, filter.to, 1, 10, ct), Times.Once);
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
            .Setup(r => r.GetPagedAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string t, DateTime? from, DateTime? endAt, int p, int ps, CancellationToken token) =>
            {
                // Точно воспроизводим логику репозитория
                var query = fakeEvents.AsQueryable();

                if (endAt.HasValue)
                {
                    if (endAt.Value.TimeOfDay == TimeSpan.Zero)
                    {
                        //Должны выполнить это
                        query = query.Where(e => e.EndAt.Date <= endAt.Value.Date);
                    }
                    else
                    {
                        query = query.Where(e => e.EndAt <= endAt.Value);
                    }
                }

                var filteredItems = query.ToList();
                return new PagedResult<Event>(filteredItems, fakeEvents.Count);
            });

        // Act
        var result = await service.GetEventsAsync(filter, ct);

        // Assert
        result.Should().NotBeNull();

        result.Events.Should().HaveCount(2);
        result.Events.Should().AllSatisfy(e => e.EndAt.Date.Should().Be(filter.to.Value.Date));

        result.EventsTotalCount.Should().Be(4);

        repositoryMock.Verify(r => r.GetPagedAsync(null, null, filter.to, 1, 10, ct), Times.Once);
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
            .Setup(r => r.GetPagedAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string t, DateTime? start, DateTime? end, int page, int pageSize, CancellationToken token) =>
            {
                // Имитируем OrderBy -> Skip -> Take как в репозитории
                var itemsOnPage = fakeEvents
                    .OrderBy(e => e.Title)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Возвращаем результат: список из 3 элементов, но TotalCount = 6
                return new PagedResult<Event>(itemsOnPage, fakeEvents.Count);
            });

        // Act
        var result = await service.GetEventsAsync(filter, ct);

        // Assert
        result.Should().NotBeNull();
        result.EventsTotalCount.Should().Be(6); // Проверяем общее количество
        result.Events.Should().HaveCount(3); // Проверяем количество в текущей выборке
        result.Events.Should().ContainSingle(e => e.Title == "Событие 5");
        result.Events.Should().BeInAscendingOrder(e => e.Title); // Проверяем сортировку, которая есть в сервисе

        repositoryMock.Verify(r => r.GetPagedAsync(null, null, null, 2, 3, ct), Times.Once);
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
        var fakeEvents = new List<Event>
        {
            Event.Create("Событие 1", targetDate, now.AddHours(5), totalSeats),
            Event.Create("Неважная встреча", now.AddHours(1), now.AddHours(5), totalSeats),
            Event.Create("Событие 3", now, now.AddHours(5), totalSeats),
            Event.Create("Поразить цель с 10 шагов", targetDate, now.AddHours(5), totalSeats),
            Event.Create("Поужинать ", now.AddHours(2), now.AddHours(5), totalSeats),
            Event.Create("Событие 6", now.AddHours(1), now.AddHours(5), totalSeats)
        };

        // Фильтр: ищем "10" и дату начала <= 3 часа от текущей
        var filter = new EventsFilter
        {
            title = "цель",
            from = targetDate,
            page = 1,
            pageSize = 10
        };

        repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken token) =>
            {
                // Имитируем логику репозитория с учетом всех фильтров
                var filtered = fakeEvents
                    .Where(e => (string.IsNullOrEmpty(title) || e.Title.Contains(title, StringComparison.OrdinalIgnoreCase)) &&
                                (!from.HasValue || e.StartAt >= from.Value))
                    .ToList();

                return new PagedResult<Event>(filtered, fakeEvents.Count);
            });

        // Act
        var result = await service.GetEventsAsync(filter, ct);

        // Assert
        result.Should().NotBeNull();

        result.EventsTotalCount.Should().Be(6);
        result.Events.Should().ContainSingle();
        result.Events.First().Title.Should().Contain("цель");
        result.Events.First().StartAt.Should().BeOnOrAfter(targetDate);

        repositoryMock.Verify(r => r.GetPagedAsync(filter.title, filter.from, null, 1, 10, ct), Times.Once);

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
        var filter = new EventsFilter { page = 2, pageSize = 2 };

        repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                filter.page,
                filter.pageSize,
                ct))
            .ReturnsAsync((string t, DateTime? start, DateTime? end, int page, int pageSize, CancellationToken token) =>
            {
                // Имитируем логику репозитория: Сортировка -> Пропуск -> Взятие
                var items = fakeEvents
                    .OrderBy(e => e.Title)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new PagedResult<Event>(items, fakeEvents.Count);
            });

        // Act
        var result = await service.GetEventsAsync(filter, ct);

        // Assert
        result.EventsTotalCount.Should().Be(6);
        result.Events.Should().HaveCount(2);

        result.Events.Should().Contain(e => e.Title.Trim().Equals("Поужинать", StringComparison.OrdinalIgnoreCase));
        result.Events.Should().Contain(e => e.Title.Equals("Событие 1", StringComparison.OrdinalIgnoreCase));

        repositoryMock.Verify(r => r.GetPagedAsync(null, null, null, 2, 2, ct), Times.Once);

    }

    [Fact]
    [Trait("Category", "GetEvents")]
    public async Task GetEvents_ShouldThrowIfCancelled()
    {
        // Arrange
        var repositoryMock = new Mock<IEventRepository>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var fakeTimeProvider = new FakeTimeProvider();

        var service = new EventService(repositoryMock.Object, loggerMock.Object, fakeTimeProvider);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Отменяем токен сразу

        var filter = new EventsFilter { page = 1, pageSize = 10 };

        // Act
        Func<Task> act = async () => await service.GetEventsAsync(filter, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        repositoryMock.Verify(r => r.GetPagedAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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
