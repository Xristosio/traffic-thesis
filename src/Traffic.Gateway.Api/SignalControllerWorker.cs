using Traffic.Application.Messaging;
using Traffic.Application.SignalStates;
using Traffic.Contracts.Enums;
using Traffic.Contracts.Messages;

namespace Traffic.Gateway.Api;

public sealed class SignalControllerWorker : BackgroundService
{
    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(1);

    private readonly IMessageConsumer<SignalDecisionCommand> _commandConsumer;
    private readonly ILogger<SignalControllerWorker> _logger;
    private readonly IMessageConsumer<TrafficMeasurement> _measurementConsumer;
    private readonly IMessagePublisher<SignalStateSnapshot> _snapshotPublisher;
    private readonly ISignalStateStore _stateStore;

    public SignalControllerWorker(
        ILogger<SignalControllerWorker> logger,
        IMessageConsumer<SignalDecisionCommand> commandConsumer,
        IMessageConsumer<TrafficMeasurement> measurementConsumer,
        IMessagePublisher<SignalStateSnapshot> snapshotPublisher,
        ISignalStateStore stateStore)
    {
        _logger = logger;
        _commandConsumer = commandConsumer;
        _measurementConsumer = measurementConsumer;
        _snapshotPublisher = snapshotPublisher;
        _stateStore = stateStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumeTask = Task.Run(
            () => ConsumeCommandsAsync(stoppingToken),
            stoppingToken);
        var measurementTask = Task.Run(
            () => ConsumeMeasurementsAsync(stoppingToken),
            stoppingToken);
        var snapshotTask = Task.Run(
            () => PublishPeriodicSnapshotsAsync(stoppingToken),
            stoppingToken);

        try
        {
            await Task.WhenAll(consumeTask, measurementTask, snapshotTask);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during host shutdown.
        }
    }

    private async Task ConsumeCommandsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var command = await _commandConsumer.ConsumeAsync(stoppingToken);
            if (command is null)
            {
                continue;
            }

            _logger.LogInformation(
                "Received command: intersection={IntersectionId} selectedSignal={SelectedSignalId} policy={Policy} green={GreenDurationSeconds} yellow={YellowDurationSeconds}",
                command.IntersectionId,
                command.SelectedSignalId,
                command.Policy,
                command.GreenDurationSeconds,
                command.YellowDurationSeconds);

            SignalStateSnapshot snapshot;
            try
            {
                snapshot = _stateStore.Apply(command, DateTimeOffset.UtcNow);
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to apply SignalDecisionCommand: intersection={IntersectionId} selectedSignal={SelectedSignalId}",
                    command.IntersectionId,
                    command.SelectedSignalId);
                continue;
            }

            _logger.LogInformation(
                "Applied state: intersection={IntersectionId} green={GreenSignalId}",
                snapshot.IntersectionId,
                GetGreenSignalId(snapshot));

            await _snapshotPublisher.PublishAsync(snapshot, stoppingToken);
        }
    }

    private async Task ConsumeMeasurementsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var measurement = await _measurementConsumer.ConsumeAsync(stoppingToken);
            if (measurement is null)
            {
                continue;
            }

            var updated = _stateStore.UpdateQueueLength(
                measurement.IntersectionId,
                measurement.SignalId,
                measurement.QueueLength);

            if (!updated)
            {
                _logger.LogWarning(
                    "Ignored queue update for unknown signal: intersection={IntersectionId} signal={SignalId} queue={QueueLength}",
                    measurement.IntersectionId,
                    measurement.SignalId,
                    measurement.QueueLength);
                continue;
            }

            _logger.LogInformation(
                "Updated queue: intersection={IntersectionId} signal={SignalId} queue={QueueLength}",
                measurement.IntersectionId,
                measurement.SignalId,
                measurement.QueueLength);
        }
    }

    private async Task PublishPeriodicSnapshotsAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SnapshotInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var snapshots = _stateStore.GetSnapshots(DateTimeOffset.UtcNow);

            foreach (var snapshot in snapshots)
            {
                _logger.LogInformation(
                    "State snapshot: intersection={IntersectionId} states={States}",
                    snapshot.IntersectionId,
                    FormatStates(snapshot));

                await _snapshotPublisher.PublishAsync(snapshot, stoppingToken);
            }
        }
    }

    private static string GetGreenSignalId(SignalStateSnapshot snapshot)
    {
        return snapshot.Signals.FirstOrDefault(signal => signal.State == SignalState.Green)?.SignalId
            ?? "none";
    }

    private static string FormatStates(SignalStateSnapshot snapshot)
    {
        return string.Join(
            ",",
            snapshot.Signals.Select(signal => $"{signal.SignalId}={signal.State}"));
    }
}
