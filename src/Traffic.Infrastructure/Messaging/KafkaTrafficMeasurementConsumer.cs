using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Traffic.Application.Messaging;
using Traffic.Contracts.Configuration;
using Traffic.Contracts.Messages;

namespace Traffic.Infrastructure.Messaging;

public sealed class KafkaTrafficMeasurementConsumer :
    IMessageConsumer<TrafficMeasurement>,
    IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaTrafficMeasurementConsumer> _logger;
    private readonly string _topicName;

    public KafkaTrafficMeasurementConsumer(
        KafkaSettings settings,
        ILogger<KafkaTrafficMeasurementConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        ValidateSettings(settings);

        _logger = logger;
        _topicName = settings.Topics.Measurements.Trim();
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = settings.BootstrapServers.Trim(),
                GroupId = settings.ConsumerGroups.DecisionEngine.Trim(),
                ClientId = "traffic-decision-engine-worker",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            })
            .Build();

        _consumer.Subscribe(_topicName);
    }

    public Task<TrafficMeasurement?> ConsumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = _consumer.Consume(cancellationToken);
            if (result?.Message is null)
            {
                return Task.FromResult<TrafficMeasurement?>(null);
            }

            TrafficMeasurement? measurement;
            try
            {
                measurement = JsonSerializer.Deserialize<TrafficMeasurement>(
                    result.Message.Value,
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to deserialize Kafka TrafficMeasurement JSON: topic={Topic} partition={Partition} offset={Offset} key={Key}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value,
                    result.Message.Key);

                CommitSkippedMessage(result);
                return Task.FromResult<TrafficMeasurement?>(null);
            }

            if (measurement is null)
            {
                _logger.LogError(
                    "Kafka TrafficMeasurement payload deserialized to null: topic={Topic} partition={Partition} offset={Offset} key={Key}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value,
                    result.Message.Key);

                CommitSkippedMessage(result);
                return Task.FromResult<TrafficMeasurement?>(null);
            }

            _consumer.Commit(result);
            return Task.FromResult<TrafficMeasurement?>(measurement);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ConsumeException exception)
        {
            _logger.LogError(
                exception,
                "Failed to consume Kafka TrafficMeasurement: topic={Topic} reason={Reason}",
                _topicName,
                exception.Error.Reason);
            return Task.FromResult<TrafficMeasurement?>(null);
        }
        catch (KafkaException exception)
        {
            _logger.LogError(
                exception,
                "Kafka error while consuming TrafficMeasurement: topic={Topic} reason={Reason}",
                _topicName,
                exception.Error.Reason);
            return Task.FromResult<TrafficMeasurement?>(null);
        }
    }

    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }

    private void CommitSkippedMessage(ConsumeResult<string, string> result)
    {
        try
        {
            _consumer.Commit(result);
        }
        catch (KafkaException exception)
        {
            _logger.LogError(
                exception,
                "Failed to commit skipped Kafka TrafficMeasurement: topic={Topic} partition={Partition} offset={Offset} reason={Reason}",
                result.Topic,
                result.Partition.Value,
                result.Offset.Value,
                exception.Error.Reason);
        }
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

        if (settings.ConsumerGroups is null ||
            string.IsNullOrWhiteSpace(settings.ConsumerGroups.DecisionEngine))
        {
            throw new InvalidOperationException(
                "Kafka ConsumerGroups DecisionEngine must be configured.");
        }
    }
}
