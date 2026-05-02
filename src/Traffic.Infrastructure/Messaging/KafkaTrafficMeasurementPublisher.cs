using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Traffic.Application.Messaging;
using Traffic.Contracts.Configuration;
using Traffic.Contracts.Messages;

namespace Traffic.Infrastructure.Messaging;

public sealed class KafkaTrafficMeasurementPublisher :
    IMessagePublisher<TrafficMeasurement>,
    IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly ILogger<KafkaTrafficMeasurementPublisher> _logger;
    private readonly IProducer<string, string> _producer;
    private readonly string _topicName;

    public KafkaTrafficMeasurementPublisher(
        KafkaSettings settings,
        ILogger<KafkaTrafficMeasurementPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        ValidateSettings(settings);

        _logger = logger;
        _topicName = settings.Topics.Measurements.Trim();
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = settings.BootstrapServers.Trim(),
                ClientId = "traffic-producer-worker",
                Acks = Acks.All
            })
            .Build();
    }

    public async Task PublishAsync(
        TrafficMeasurement message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            var key = GetMessageKey(message);
            var value = JsonSerializer.Serialize(message, JsonOptions);

            var result = await _producer.ProduceAsync(
                _topicName,
                new Message<string, string>
                {
                    Key = key,
                    Value = value
                },
                cancellationToken);

            _logger.LogInformation(
                "Published TrafficMeasurement to Kafka: topic={Topic} key={Key} partition={Partition} offset={Offset}",
                result.Topic,
                key,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ProduceException<string, string> exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish TrafficMeasurement to Kafka: topic={Topic} key={Key} reason={Reason}",
                _topicName,
                GetMessageKey(message),
                exception.Error.Reason);
            throw;
        }
        catch (KafkaException exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish TrafficMeasurement to Kafka: topic={Topic} key={Key} reason={Reason}",
                _topicName,
                GetMessageKey(message),
                exception.Error.Reason);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish TrafficMeasurement to Kafka: topic={Topic} key={Key}",
                _topicName,
                GetMessageKey(message));
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }

    private static string GetMessageKey(TrafficMeasurement message)
    {
        return $"{message.IntersectionId}:{message.SignalId}";
    }

    private static void ValidateSettings(KafkaSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka BootstrapServers must be configured.");
        }

        if (settings.Topics is null || string.IsNullOrWhiteSpace(settings.Topics.Measurements))
        {
            throw new InvalidOperationException("Kafka Topics Measurements must be configured.");
        }
    }
}
