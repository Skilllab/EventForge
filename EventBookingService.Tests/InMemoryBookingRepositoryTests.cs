//using Microsoft.Extensions.Time.Testing;

//namespace EventBookingService.Tests
//{
//    public class InMemoryBookingRepositoryTests
//    {
//        [Fact]
//        public async Task AddAsync_ShouldSaveBooking_WhenDataIsValid()
//        {
//            // Arrange
//            var repository = new InMemoryBookingRepository();
//            var fakeTimeProvider = new FakeTimeProvider();
//            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
//            fakeTimeProvider.SetUtcNow(fixedUtcNow);
//            var now = fixedUtcNow.UtcDateTime;

//            var booking = Booking.Create(Guid.NewGuid(), now);
//            var ct = CancellationToken.None;

//            // Act
//            await repository.AddAsync(booking, ct);

//            // Assert
//            var result = await repository.GetByIdAsync(booking.Id, ct);
//            result.Should().NotBeNull();
//            result.Id.Should().Be(booking.Id);
//        }

//        [Fact]
//        public async Task DeleteAsync_ShouldReturnTrue_WhenBookingExists()
//        {
//            // Arrange
//            var repository = new InMemoryBookingRepository();
//            var fakeTimeProvider = new FakeTimeProvider();
//            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
//            fakeTimeProvider.SetUtcNow(fixedUtcNow);
//            var now = fixedUtcNow.UtcDateTime;

//            var booking = Booking.Create(Guid.NewGuid(), now);
//            await repository.AddAsync(booking, CancellationToken.None);

//            // Act
//            var isDeleted = await repository.DeleteAsync(booking.Id, CancellationToken.None);

//            // Assert
//            isDeleted.Should().BeTrue();
//            var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);
//            result.Should().BeNull();
//        }

//        [Fact]
//        public async Task GetByIdAsync_ShouldReturnNull_WhenIdIsUnknown()
//        {
//            // Arrange
//            var repository = new InMemoryBookingRepository();
//            var randomId = Guid.NewGuid();

//            // Act
//            var result = await repository.GetByIdAsync(randomId, CancellationToken.None);

//            // Assert
//            result.Should().BeNull();
//        }

//        [Fact]
//        public async Task UpdateAsync_ShouldOverwriteExistingBooking()
//        {
//            // Arrange
//            var repository = new InMemoryBookingRepository();
//            var fakeTimeProvider = new FakeTimeProvider();
//            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
//            fakeTimeProvider.SetUtcNow(fixedUtcNow);
//            var now = fixedUtcNow.UtcDateTime;

//            var eventId = Guid.NewGuid();
//            var booking = Booking.Create(eventId, now);
//            var date = DateTime.UtcNow;
//            await repository.AddAsync(booking, CancellationToken.None);

//            booking.Status = BookingStatus.Confirmed;
//            booking.ProcessedAt = date;

//            // Act
//            await repository.UpdateAsync(booking, CancellationToken.None);

//            // Assert
//            var updated = await repository.GetByIdAsync(booking.Id, CancellationToken.None);
//            updated.Should().NotBeNull();
//            updated!.Status.Should().Be(BookingStatus.Confirmed);
//            updated.ProcessedAt.Should().Be(date);
//        }

//        [Fact]
//        public async Task GetAll_ShouldReturnOnlyFilteredItems()
//        {
//            // Arrange
//            var repository = new InMemoryBookingRepository();
//            var targetEventId = Guid.NewGuid();
//            var otherEventId = Guid.NewGuid();
//            var fakeTimeProvider = new FakeTimeProvider();
//            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
//            fakeTimeProvider.SetUtcNow(fixedUtcNow);
//            var now = fixedUtcNow.UtcDateTime;


//            var booking1 = Booking.Create(targetEventId, now);
//            var booking2 = Booking.Create(otherEventId, now);

//            await repository.AddAsync(booking1, CancellationToken.None);
//            await repository.AddAsync(booking2, CancellationToken.None);

//            // Act
//            var result = repository.GetAll(b => b.EventId == targetEventId, CancellationToken.None);

//            // Assert
//            result.Should().ContainSingle();
//            result.First().EventId.Should().Be(targetEventId);
//        }

//        [Fact]
//        public async Task AllMethods_ShouldThrow_WhenCancellationTokenIsCancelled()
//        {
//            // Arrange
//            var repository = new InMemoryBookingRepository();
//            var fakeTimeProvider = new FakeTimeProvider();
//            var fixedUtcNow = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
//            fakeTimeProvider.SetUtcNow(fixedUtcNow);
//            var now = fixedUtcNow.UtcDateTime;

//            var booking = Booking.Create(Guid.NewGuid(), now);
//            using var cts = new CancellationTokenSource();
//            await cts.CancelAsync();

//            // Act & Assert
//            await Assert.ThrowsAsync<OperationCanceledException>(() => repository.AddAsync(booking, cts.Token));
//            await Assert.ThrowsAsync<OperationCanceledException>(() => repository.GetByIdAsync(Guid.NewGuid(), cts.Token));
//            await Assert.ThrowsAsync<OperationCanceledException>(() => repository.DeleteAsync(Guid.NewGuid(), cts.Token));
//            Assert.Throws<OperationCanceledException>(() => repository.GetAll(null, cts.Token));
//        }
//    }
//}
