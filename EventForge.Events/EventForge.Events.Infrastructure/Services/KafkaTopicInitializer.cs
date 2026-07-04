using Confluent.Kafka;
using Confluent.Kafka.Admin;

using EventForge.Contract.Brokers;
using EventForge.Events.Infrastructure.Common;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Events.Infrastructure.Services
{
    /// <summary>
    /// Гарантирует создание нужных Kafka-топиков при старте сервиса.
    /// </summary>
    public class KafkaTopicInitializer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaTopicInitializer> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var config = new AdminClientConfig
                {
                    BootstrapServers = options.Value.BootstrapServers
                };

                using var adminClient = new AdminClientBuilder(config).Build();

                await adminClient.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = TopicNames.BookingConfirmed,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    }
                ]);
            }
            catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == Confluent.Kafka.ErrorCode.TopicAlreadyExists))
            {
                logger.LogInformation("Kafka topic {Topic} уже существует", TopicNames.BookingConfirmed);
            }
            catch (Exception ex)
            {
                // Не валим сервис, если не получилось создать топик.
                logger.LogError(ex, "Не удалось создать Kafka topic {Topic}", TopicNames.BookingConfirmed);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
