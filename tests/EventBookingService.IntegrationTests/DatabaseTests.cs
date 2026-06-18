using EventBookingService.Domain.Entities;
using EventBookingService.Infrastructure.Mapping;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace EventBookingService.IntegrationTests
{
    public class DatabaseTests : BaseRepositoryTest
    {
        [Fact]
        public async Task Migrations_Should_Apply_Successfully()
        {
            // Arrange
            await ResetDatabaseAsync();
            var context = await Factory.CreateDbContextAsync(CancellationToken.None);
            await context.Database.EnsureDeletedAsync(CancellationToken.None);

            //Act
            await context.Database.MigrateAsync(CancellationToken.None);

            // Assert
            //Если ничего не упало, значит тест прошёл
            Assert.True(true);
        }

        [Fact]
        public async Task Booking_Should_Fail_If_Event_Does_Not_Exist()
        {
            // Arrange
            await using var context = await CreateContext();
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var fakeNow = fixedUtcNow.UtcDateTime;
            var userId = Guid.NewGuid();
            var booking = Booking.Create(Guid.NewGuid(), userId, fakeNow);

            // Act
            context.Bookings.Add(booking.ToEntity());

            // Assert
            // Проверяем, что БД выбросила DbUpdateException (из-за FK violation)
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(CancellationToken.None));
        }

        [Fact]
        public async Task Deleting_Event_Should_Delete_Bookings_Cascade()
        {
            // Arrange
            await using var context = await CreateContext();
            var defaultTitle = "Межпланетная конференция .NET";
            var defaultSeats = 100;
            var defaultDescription = "Первая в своём роде конференция таких масштабов";
            var fakeTimeProvider = new FakeTimeProvider();
            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            fakeTimeProvider.SetUtcNow(fixedUtcNow);
            var now = fixedUtcNow.UtcDateTime;
            // Используем dummy userId из миграции, который гарантированно существует в БД
            var dummyUserId = new Guid("11111111-1111-1111-1111-111111111111");
            var startDate = now;
            var endDate = now.AddDays(2);
            var @event = Event.Create(defaultTitle, startDate, endDate, defaultSeats, defaultDescription);
            context.Events.Add(@event.ToEntity());
            var booking = Booking.Create(@event.Id, dummyUserId, now);
            context.Bookings.Add(booking.ToEntity());
            await context.SaveChangesAsync(CancellationToken.None);

            // Act
            var eventFromBase = context.Events.First();
            context.Events.Remove(eventFromBase);
            await context.SaveChangesAsync(CancellationToken.None);


            // Assert
            Assert.Empty(context.Bookings.Where(b => b.Id.Equals(booking.Id)));
        }
    }
}
