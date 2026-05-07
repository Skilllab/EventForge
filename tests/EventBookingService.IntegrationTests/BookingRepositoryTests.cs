using EventBookingService.Data.Repositories;
using EventBookingService.Domain.Entities;
using EventBookingService.Domain.Interfaces;

using FluentAssertions;

namespace EventBookingService.IntegrationTests;

public class BookingRepositoryTests : BaseRepositoryTest
{
    private IBookingRepository CreateBookingRepo() => new BookingRepository( Factory);
    private IEventRepository CreateEventRepo() => new EventRepository(Factory);

    [Fact]
    public async Task AddAsync_ShouldSaveBooking_WhenEventExists()
    {
        // Arrange
        await ResetDatabaseAsync();
        var eventRepo = CreateEventRepo();
        var bookingRepo = CreateBookingRepo();
     
        var totalSeats = 100;
        var title = "Очередное суперсобытие";

        var @event = Event.Create(title, fakeNow, fakeNow.AddDays(1), totalSeats);
        await eventRepo.AddAsync(@event, CancellationToken.None);

        var booking = Booking.Create(@event.Id, fakeNow);

        // Act
        await bookingRepo.AddAsync(booking, CancellationToken.None);

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

        var totalSeats = 100;
        var title = "Очередное суперсобытие";

        var @event = Event.Create(title, fakeNow, fakeNow.AddDays(1), totalSeats);
        await eventRepo.AddAsync(@event, CancellationToken.None);

        var booking = Booking.Create(@event.Id, fakeNow);
        await bookingRepo.AddAsync(booking, CancellationToken.None);

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

        var totalSeats = 100;
        var title = "Очередное суперсобытие";

        var @event = Event.Create(title, fakeNow, fakeNow.AddDays(1), totalSeats);
        await eventRepo.AddAsync(@event, CancellationToken.None);

        var b1 = Booking.Create(@event.Id, fakeNow);
        var b2 = Booking.Create(@event.Id, fakeNow);
        b2.Reject(fakeNow);

        await bookingRepo.AddAsync(b1, CancellationToken.None);
        await bookingRepo.AddAsync(b2, CancellationToken.None);

        // Act
        var rejectedBookings = await bookingRepo.GetAll(BookingStatus.Rejected, CancellationToken.None);

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
       
        var totalSeats = 100;
        var title = "Очередное суперсобытие";

        var @event = Event.Create(title, fakeNow, fakeNow.AddDays(1), totalSeats);
        await eventRepo.AddAsync(@event, CancellationToken.None);

        var booking = Booking.Create(@event.Id, fakeNow);
        await bookingRepo.AddAsync(booking, CancellationToken.None);

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
}
