using EventBookingService.Application.Interfaces;
using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Exceptions;
using EventBookingService.Infrastructure.Repositories;
using EventBookingService.Infrastructure.Services;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace EventBookingService.IntegrationTests;

public class BookingRepositoryTests : BaseRepositoryTest
{
    private IBookingRepository CreateBookingRepo() => new BookingRepository(Factory);
    private IEventRepository CreateEventRepo() => new EventRepository(Factory);

    [Fact]
    public async Task AddAsync_ShouldSaveBooking_WhenEventExists()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventRepo = CreateEventRepo();
        var bookingRepo = CreateBookingRepo();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var fakeNow = fixedUtcNow.UtcDateTime;
        var totalSeats = 100;
        var title = "Очередное суперсобытие";
        var userId = Guid.NewGuid();

        var @event = Event.Create(title, fakeNow, fakeNow.AddDays(1), totalSeats);
        await eventRepo.AddAsync(@event, CancellationToken.None);

        var booking = Booking.Create(@event.Id, userId, fakeNow);

        // Act

        var transactionService = new TransactionService(Factory);
        var ct = CancellationToken.None;

        await transactionService.ExecuteAsync(async (txContext) =>
        {
            await bookingRepo.AddInContextAsync(booking, txContext.DbContext, ct);
        }, ct);

        // Assert
        var result = await bookingRepo.GetByIdAsync(booking.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(@event.Id);
        result.Status.Should().Be(BookingStatus.Pending);
    }
   

    [Fact]
    public async Task UpdateAsync_ShouldChangeStatusAndProcessedDate()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventRepo = CreateEventRepo();
        var bookingRepo = CreateBookingRepo();
        var userId = Guid.NewGuid();

        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var fakeNow = fixedUtcNow.UtcDateTime;
        var totalSeats = 100;
        var title = "Очередное суперсобытие";

        var @event = Event.Create(title, fakeNow, fakeNow.AddDays(1), totalSeats);
        await eventRepo.AddAsync(@event, CancellationToken.None);

        var booking = Booking.Create(@event.Id, userId, fakeNow);

        var transactionService = new TransactionService(Factory);
        var ct = CancellationToken.None;

        await transactionService.ExecuteAsync(async (txContext) =>
        {
            await bookingRepo.AddInContextAsync(booking, txContext.DbContext, ct);
        }, ct);


        // Act
        booking.Confirm(fakeNow.AddHours(1));
        await bookingRepo.UpdateAsync(booking, CancellationToken.None);

        // Assert
        var updated = await bookingRepo.GetByIdAsync(booking.Id, CancellationToken.None);

        updated.Should().NotBeNull();
        updated.Status.Should().Be(BookingStatus.Confirmed);
        updated.ProcessedAt.Should().BeCloseTo(fakeNow.AddHours(1), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlyRequestedStatus()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventRepo = CreateEventRepo();
        var bookingRepo = CreateBookingRepo();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var fakeNow = fixedUtcNow.UtcDateTime;
        var totalSeats = 100;
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var title = "Очередное суперсобытие";

        var @event = Event.Create(title, fakeNow, fakeNow.AddDays(1), totalSeats);
        await eventRepo.AddAsync(@event, CancellationToken.None);

        var b1 = Booking.Create(@event.Id, userId1, fakeNow);
        var b2 = Booking.Create(@event.Id, userId2, fakeNow);
        b2.Reject(fakeNow);

        var transactionService = new TransactionService(Factory);
        var ct = CancellationToken.None;

        await transactionService.ExecuteAsync(async (txContext) =>
        {
            await bookingRepo.AddInContextAsync(b1, txContext.DbContext, ct);

            return true;
        }, ct);


        await transactionService.ExecuteAsync(async (txContext) =>
        {
            // Получаем событие с блокировкой FOR UPDATE внутри транзакции
            var eventInTx = await eventRepo.GetByIdWithLockInContextAsync(@event.Id, txContext.DbContext, ct);
            eventInTx.Should().NotBeNull();

            // Проверяем и резервируем место (все в бизнес-слое)
            if (!eventInTx.TryReserveSeats())
            {
                throw new NoAvailableSeatsException(nameof(Event), eventInTx.Id.ToString());
            }

            // Все операции сохранения в рамках одной транзакции
            await eventRepo.UpdateInContextAsync(eventInTx, txContext.DbContext, ct);
            await bookingRepo.AddInContextAsync(b2, txContext.DbContext, ct);

            return true;
        }, ct);

        // Act
        var rejectedBookings = await bookingRepo.GetAllAsync(BookingStatus.Rejected, CancellationToken.None);

        // Assert
        rejectedBookings.Should().HaveCount(1);
        rejectedBookings.First().Id.Should().Be(b2.Id);
        rejectedBookings.Should().AllSatisfy(b => b.Status.Should().Be(BookingStatus.Rejected));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenBookingExists()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventRepo = CreateEventRepo();
        var bookingRepo = CreateBookingRepo();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var fakeNow = fixedUtcNow.UtcDateTime;
        var totalSeats = 100;
        var userId = Guid.NewGuid();

        var title = "Очередное суперсобытие";

        var @event = Event.Create(title, fakeNow, fakeNow.AddDays(1), totalSeats);
        await eventRepo.AddAsync(@event, CancellationToken.None);

        var booking = Booking.Create(@event.Id, userId, fakeNow);

        var transactionService = new TransactionService(Factory);
        var ct = CancellationToken.None;


        await transactionService.ExecuteAsync(async (txContext) =>
        {
            await bookingRepo.AddInContextAsync(booking, txContext.DbContext, ct);
        }, ct);


        // Act
        var result = await bookingRepo.DeleteAsync(booking.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var exist = await bookingRepo.GetByIdAsync(booking.Id, CancellationToken.None);
        exist.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenBookingDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repo = CreateBookingRepo();

        // Act
        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenBookingDoesNotExist()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repo = CreateBookingRepo();

        // Act & Assert
        Func<Task<bool>> act = async () => await repo.DeleteAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().NotThrowAsync();

        var secondDeleteResult = await act();
        secondDeleteResult.Should().BeFalse();
    }

    [Fact]
    public async Task OverbookingProtection_ShouldAllowExactlyTwoBookings_WhenEventHasTwoSeatsAndThreeRequests()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventRepo = CreateEventRepo();
        var bookingRepo = CreateBookingRepo();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var fakeNow = fixedUtcNow.UtcDateTime;
        var totalSeats = 2; // Всего 2 места
        var title = "Событие с 2 местами";

        var @event = Event.Create(title, fakeNow, fakeNow.AddDays(1), totalSeats);
        await eventRepo.AddAsync(@event, CancellationToken.None);

        var transactionService = new TransactionService(Factory);
        var ct = CancellationToken.None;

        // Act - пытаемся создать 3 бронирования одновременно (параллельно)
        // Каждое бронирование в отдельной транзакции:
        // 1. Получаем Event с блокировкой (FOR UPDATE)
        // 2. Вызываем TryReserveSeats() - уменьшаем AvailableSeats
        // 3. Создаем Booking и сохраняем оба (Event + Booking) в одной транзакции
        // 4. Если нет мест - выбрасываем исключение

        var successfulBookings = new List<Booking>();
        var failedAttempts = 0;

        var tasks = Enumerable.Range(1, 3).Select(i =>
            Task.Run(async () =>
            {
                try
                {
                    var userId = Guid.NewGuid();
                    var booking = Booking.Create(@event.Id, userId, fakeNow);

                    await transactionService.ExecuteAsync(async (txContext) =>
                    {
                        // Получаем событие с блокировкой FOR UPDATE внутри транзакции
                        var eventInTx = await eventRepo.GetByIdWithLockInContextAsync(@event.Id, txContext.DbContext, ct);
                        eventInTx.Should().NotBeNull();

                        // Проверяем и резервируем место (все в бизнес-слое)
                        if (!eventInTx!.TryReserveSeats())
                        {
                            throw new NoAvailableSeatsException(nameof(Event), eventInTx.Id.ToString());
                        }

                        // Все операции сохранения в рамках одной транзакции
                        await eventRepo.UpdateInContextAsync(eventInTx, txContext.DbContext, ct);
                        await bookingRepo.AddInContextAsync(booking, txContext.DbContext, ct);

                        return true;
                    }, ct);

                    lock (successfulBookings)
                    {
                        successfulBookings.Add(booking);
                    }
                }
                catch (NoAvailableSeatsException)
                {
                    Interlocked.Increment(ref failedAttempts);
                }
            })
        ).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        // Должно быть успешно создано ровно 2 бронирования (так как 2 места)
        successfulBookings.Should().HaveCount(2);
        failedAttempts.Should().Be(1); // Одна попытка должна не пройти

        // Проверяем, что оба успешных бронирования в БД
        foreach (var booking in successfulBookings)
        {
            var result = await bookingRepo.GetByIdAsync(booking.Id, CancellationToken.None);
            result.Should().NotBeNull();
            result.Id.Should().Be(booking.Id);
            result.EventId.Should().Be(@event.Id);
            result.Status.Should().Be(BookingStatus.Pending);
        }

        // Проверяем состояние Event - AvailableSeats должны быть = 0
        var updatedEvent = await eventRepo.GetByIdAsync(@event.Id, CancellationToken.None);
        updatedEvent.Should().NotBeNull();
        updatedEvent.AvailableSeats.Should().Be(0); // Все места зарезервированы
        updatedEvent.TotalSeats.Should().Be(2);

        //// Все бронирования в БД должны быть Pending
        //var allBookings = await bookingRepo.GetAllAsync(BookingStatus.Pending, CancellationToken.None);
        //allBookings.Should().HaveCount(2);
        //allBookings.All(b => b.EventId == @event.Id).Should().BeTrue();
    }

    [Fact]
    public async Task AddInContextAsync_WithinSingleTransaction_ShouldSaveMultipleBookings()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventRepo = CreateEventRepo();
        var bookingRepo = CreateBookingRepo();
        var fakeTimeProvider = new FakeTimeProvider();
        var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(fixedUtcNow);
        var fakeNow = fixedUtcNow.UtcDateTime;
        var totalSeats = 3;
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var title = "Событие с 3 местами для одной транзакции";

        var @event = Event.Create(title, fakeNow, fakeNow.AddDays(1), totalSeats);
        await eventRepo.AddAsync(@event, CancellationToken.None);

        
        var booking1 = Booking.Create(@event.Id, userId1, fakeNow);
        var booking2 = Booking.Create(@event.Id, userId2, fakeNow);

        // Act - создаем несколько бронирований в одной транзакции используя AddInContextAsync
        var transactionService = new TransactionService(Factory);
        var ct = CancellationToken.None;

        await transactionService.ExecuteAsync(async (txContext) =>
        {
            // Добавляем оба бронирования в одной транзакции
            await bookingRepo.AddInContextAsync(booking1, txContext.DbContext, ct);
            await bookingRepo.AddInContextAsync(booking2, txContext.DbContext, ct);
        }, ct);

        // Assert - проверяем, что оба бронирования созданы в БД
        var result1 = await bookingRepo.GetByIdAsync(booking1.Id, CancellationToken.None);
        var result2 = await bookingRepo.GetByIdAsync(booking2.Id, CancellationToken.None);

        result1.Should().NotBeNull();
        result1.Id.Should().Be(booking1.Id);
        result1.EventId.Should().Be(@event.Id);
        result1.Status.Should().Be(BookingStatus.Pending);

        result2.Should().NotBeNull();
        result2.Id.Should().Be(booking2.Id);
        result2.EventId.Should().Be(@event.Id);
        result2.Status.Should().Be(BookingStatus.Pending);

        // Verify - оба бронирования с разными ID
        new[] { result1.Id, result2.Id }.Distinct().Should().HaveCount(2);
    }
}
