using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Traffic.Application.Messaging;
using Traffic.Contracts.Configuration;
using Traffic.Contracts.Messages;

namespace Traffic.Infrastructure.Messaging;

public sealed class KafkaSignalDecisionCommandConsumer :
    IMessageConsumer<SignalDecisionCommand>,
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
    private readonly ILogger<KafkaSignalDecisionCommandConsumer> _logger;
    private readonly string _topicName;

    public KafkaSignalDecisionCommandConsumer(
        KafkaSettings settings,
        ILogger<KafkaSignalDecisionCommandConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        ValidateSettings(settings);

        _logger = logger;
        _topicName = settings.Topics.Commands.Trim();
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = settings.BootstrapServers.Trim(),
                GroupId = settings.ConsumerGroups.Gateway.Trim(),
                ClientId = "traffic-gateway-api",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            })
            .Build();

        _consumer.Subscribe(_topicName);
    }

    public Task<SignalDecisionCommand?> ConsumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = _consumer.Consume(cancellationToken);
            if (result?.Message is null)
            {
                return Task.FromResult<SignalDecisionCommand?>(null);
            }

            SignalDecisionCommand? command;
            try
            {
                command = JsonSerializer.Deserialize<SignalDecisionCommand>(
                    result.Message.Value,
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to deserialize Kafka SignalDecisionCommand JSON: topic={Topic} partition={Partition} offset={Offset} key={Key}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value,
                    result.Message.Key);

                CommitSkippedMessage(result);
                return Task.FromResult<SignalDecisionCommand?>(null);
            }

            if (command is null)
            {
                _logger.LogError(
                    "Kafka SignalDecisionCommand payload deserialized to null: topic={Topic} partition={Partition} offset={Offset} key={Key}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value,
                    result.Message.Key);

                CommitSkippedMessage(result);
                return Task.FromResult<SignalDecisionCommand?>(null);
            }

            _consumer.Commit(result);
            return Task.FromResult<SignalDecisionCommand?>(command);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ConsumeException exception)
        {
            _logger.LogError(
                exception,
                "Failed to consume Kafka SignalDecisionCommand: topic={Topic} reason={Reason}",
                _topicName,
                exception.Error.Reason);
            return Task.FromResult<SignalDecisionCommand?>(null);
        }
        catch (KafkaException exception)
        {
            _logger.LogError(
                exception,
                "Kafka error while consuming SignalDecisionCommand: topic={Topic} reason={Reason}",
                _topicName,
                exception.Error.Reason);
            return Task.FromResult<SignalDecisionCommand?>(null);
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
                "Failed to commit skipped Kafka SignalDecisionCommand: topic={Topic} partition={Partition} offset={Offset} reason={Reason}",
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

        if (settings.Topics is null || string.IsNullOrWhiteSpace(settings.Topics.Commands))
        {
            throw new InvalidOperationException("Kafka Topics Commands must be configured.");
        }

        if (settings.ConsumerGroups is null ||
            string.IsNullOrWhiteSpace(settings.ConsumerGroups.Gateway))
        {
            throw new InvalidOperationException("Kafka ConsumerGroups Gateway must be configured.");
        }
    }
}
