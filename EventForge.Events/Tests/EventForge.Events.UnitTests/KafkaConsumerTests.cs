using EventForge.Contract.Brokers;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Domain.Exceptions;
using EventForge.Events.Infrastructure.Common;
using EventForge.Events.Infrastructure.Services;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace EventForge.Events.UnitTests;

public class KafkaConsumerTests
{
    [Fact]
    public async Task BookingConfirmedConsumer_Should_Add_Message_When_Reservation_Succeeds()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventServiceMock = new Mock<IEventService>();
        var message = new BookingConfirmed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        eventServiceMock
            .Setup(x => x.TryReserveSeatAsync(message.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventServiceMock.Object);
        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        using var consumer = new BookingConfirmedConsumer(
            scopeFactory,
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingConfirmedConsumer>>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        processedRepositoryMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingConfirmed), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookingConfirmedConsumer_Should_Not_Add_Message_When_Already_Processed()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventServiceMock = new Mock<IEventService>();
        var message = new BookingConfirmed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventServiceMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingConfirmedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingConfirmedConsumer>>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        eventServiceMock.Verify(x => x.TryReserveSeatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        processedRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BookingConfirmedConsumer_Should_Mark_Message_When_Event_Not_Found()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventServiceMock = new Mock<IEventService>();
        var message = new BookingConfirmed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        eventServiceMock
            .Setup(x => x.TryReserveSeatAsync(message.EventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Event", message.EventId.ToString()));

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventServiceMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingConfirmedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingConfirmedConsumer>>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        processedRepositoryMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingConfirmed), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookingRejectedConsumer_Should_Add_Message_When_Not_Processed_Yet()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var message = BookingRejected.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        services.AddSingleton(processedRepositoryMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingRejectedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingRejectedConsumer>>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        processedRepositoryMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingRejected), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookingCancelledConsumer_Should_Add_Message_After_Releasing_Seat()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventServiceMock = new Mock<IEventService>();
        var message = BookingCancelled.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        eventServiceMock
            .Setup(x => x.ReleaseSeatAsync(message.EventId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventServiceMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingCancelledConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingCancelledConsumer>>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        eventServiceMock.Verify(x => x.ReleaseSeatAsync(message.EventId, It.IsAny<CancellationToken>()), Times.Once);
        processedRepositoryMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingCancelled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookingCancelledConsumer_Should_Still_Mark_Message_When_Event_Not_Found()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventServiceMock = new Mock<IEventService>();
        var message = BookingCancelled.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        eventServiceMock
            .Setup(x => x.ReleaseSeatAsync(message.EventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Event", message.EventId.ToString()));

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventServiceMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingCancelledConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingCancelledConsumer>>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        processedRepositoryMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingCancelled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookingConfirmedConsumer_Should_Ignore_Null_Message()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventServiceMock = new Mock<IEventService>();

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventServiceMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingConfirmedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingConfirmedConsumer>>());

        await consumer.HandleMessageAsync(null, CancellationToken.None);

        processedRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        eventServiceMock.Verify(x => x.TryReserveSeatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
