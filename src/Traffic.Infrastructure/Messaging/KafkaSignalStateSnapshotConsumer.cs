using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Traffic.Application.Messaging;
using Traffic.Contracts.Configuration;
using Traffic.Contracts.Messages;

namespace Traffic.Infrastructure.Messaging;

public sealed class KafkaSignalStateSnapshotConsumer :
    IMessageConsumer<SignalStateSnapshot>,
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
    private readonly ILogger<KafkaSignalStateSnapshotConsumer> _logger;
    private readonly string _topicName;

    public KafkaSignalStateSnapshotConsumer(
        KafkaSettings settings,
        ILogger<KafkaSignalStateSnapshotConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        ValidateSettings(settings);

        _logger = logger;
        _topicName = settings.Topics.State.Trim();
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = settings.BootstrapServers.Trim(),
                GroupId = settings.ConsumerGroups.ProducerState.Trim(),
                ClientId = "traffic-producer-worker-state",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            })
            .Build();

        _consumer.Subscribe(_topicName);
    }

    public Task<SignalStateSnapshot?> ConsumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = _consumer.Consume(cancellationToken);
            if (result?.Message is null)
            {
                return Task.FromResult<SignalStateSnapshot?>(null);
            }

            SignalStateSnapshot? snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<SignalStateSnapshot>(
                    result.Message.Value,
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to deserialize Kafka SignalStateSnapshot JSON: topic={Topic} partition={Partition} offset={Offset} key={Key}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value,
                    result.Message.Key);

                CommitSkippedMessage(result);
                return Task.FromResult<SignalStateSnapshot?>(null);
            }

            if (snapshot is null)
            {
                _logger.LogError(
                    "Kafka SignalStateSnapshot payload deserialized to null: topic={Topic} partition={Partition} offset={Offset} key={Key}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value,
                    result.Message.Key);

                CommitSkippedMessage(result);
                return Task.FromResult<SignalStateSnapshot?>(null);
            }

            _consumer.Commit(result);
            return Task.FromResult<SignalStateSnapshot?>(snapshot);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ConsumeException exception)
        {
            _logger.LogError(
                exception,
                "Failed to consume Kafka SignalStateSnapshot: topic={Topic} reason={Reason}",
                _topicName,
                exception.Error.Reason);
            return Task.FromResult<SignalStateSnapshot?>(null);
        }
        catch (KafkaException exception)
        {
            _logger.LogError(
                exception,
                "Kafka error while consuming SignalStateSnapshot: topic={Topic} reason={Reason}",
                _topicName,
                exception.Error.Reason);
            return Task.FromResult<SignalStateSnapshot?>(null);
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
                "Failed to commit skipped Kafka SignalStateSnapshot: topic={Topic} partition={Partition} offset={Offset} reason={Reason}",
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

        if (settings.Topics is null || string.IsNullOrWhiteSpace(settings.Topics.State))
        {
            throw new InvalidOperationException("Kafka Topics State must be configured.");
        }

        if (settings.ConsumerGroups is null ||
            string.IsNullOrWhiteSpace(settings.ConsumerGroups.ProducerState))
        {
            throw new InvalidOperationException(
                "Kafka ConsumerGroups ProducerState must be configured.");
        }
    }
}
