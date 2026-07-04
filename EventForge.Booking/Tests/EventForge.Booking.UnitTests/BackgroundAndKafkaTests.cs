using Confluent.Kafka;

using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Services;
using EventForge.Contract.Brokers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Moq;

namespace EventForge.Booking.UnitTests;

public class BackgroundAndKafkaTests
{
    [Fact]
    public async Task KafkaBookingConfirmedPublisher_Should_Publish_Raw_Message_With_Expected_Topic_And_Key()
    {
        // Arrange
        var producerMock = new Mock<IProducer<string, string>>();
        var loggerMock = new Mock<ILogger<KafkaBookingConfirmedPublisher>>();
        using var publisher = new KafkaBookingConfirmedPublisher(producerMock.Object, loggerMock.Object);

        const string topic = TopicNames.BookingConfirmed;
        const string key = "event-key";
        const string payload = """{"eventId":"11111111-1111-1111-1111-111111111111"}""";

        producerMock
            .Setup(x => x.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryResult<string, string>) null!);

        // Act
        await publisher.PublishRawAsync(topic, key, payload, CancellationToken.None);

        // Assert
        producerMock.Verify(x => x.ProduceAsync(
            topic,
            It.Is<Message<string, string>>(m => m.Key == key && m.Value == payload),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OutboxPublisherBackgroundService_Should_Mark_Message_Processed_When_Publish_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        var outboxRepositoryMock = new Mock<IOutboxRepository>();
        var publisherMock = new Mock<IBookingConfirmedPublisher>();
        var loggerMock = new Mock<ILogger<OutboxPublisherBackgroundService>>();
        var message = OutboxMessage.Create("BookingConfirmed", TopicNames.BookingConfirmed, "event-key", "payload", DateTime.UtcNow, null);

        outboxRepositoryMock
            .Setup(x => x.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);
        publisherMock
            .Setup(x => x.PublishRawAsync(message.Topic, message.MessageKey, message.Payload, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(outboxRepositoryMock.Object);
        services.AddSingleton(publisherMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var service = new OutboxPublisherBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            loggerMock.Object,
            new FakeTimeProvider());

        // Act
        await service.ProcessOnceAsync(CancellationToken.None);

        // Assert
        outboxRepositoryMock.Verify(x => x.MarkProcessedAsync(message.Id, It.IsAny<CancellationToken>()), Times.Once);
        outboxRepositoryMock.Verify(x => x.MarkFailedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OutboxPublisherBackgroundService_Should_Mark_Message_Failed_When_Publish_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        var outboxRepositoryMock = new Mock<IOutboxRepository>();
        var publisherMock = new Mock<IBookingConfirmedPublisher>();
        var loggerMock = new Mock<ILogger<OutboxPublisherBackgroundService>>();
        var message = OutboxMessage.Create("BookingConfirmed", TopicNames.BookingConfirmed, "event-key", "payload", DateTime.UtcNow, null);

        outboxRepositoryMock
            .Setup(x => x.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);
        publisherMock
            .Setup(x => x.PublishRawAsync(message.Topic, message.MessageKey, message.Payload, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("kafka failed"));

        services.AddSingleton(outboxRepositoryMock.Object);
        services.AddSingleton(publisherMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var service = new OutboxPublisherBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            loggerMock.Object,
            new FakeTimeProvider());

        // Act
        await service.ProcessOnceAsync(CancellationToken.None);

        // Assert
        outboxRepositoryMock.Verify(x => x.MarkFailedAsync(message.Id, It.Is<string>(s => s.Contains("kafka failed")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookingBackgroundService_Should_Invoke_UpdateBookingAsync()
    {
        // Arrange
        var services = new ServiceCollection();
        var bookingServiceMock = new Mock<IBookingService>();
        var loggerMock = new Mock<ILogger<BookingBackgroundService>>();

        bookingServiceMock
            .Setup(x => x.UpdateBookingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(bookingServiceMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var service = new BookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            loggerMock.Object,
            new FakeTimeProvider());

        // Act
        await service.ProcessOnceAsync(CancellationToken.None);

        // Assert
        bookingServiceMock.Verify(x => x.UpdateBookingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
