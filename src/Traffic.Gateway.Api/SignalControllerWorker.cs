using Traffic.Application.Messaging;
using Traffic.Application.Persistence;
using Traffic.Application.SignalStates;
using Traffic.Contracts.Configuration;
using Traffic.Contracts.Enums;
using Traffic.Contracts.Messages;

namespace Traffic.Gateway.Api;

public sealed class SignalControllerWorker : BackgroundService
{
    private const string UnknownPolicy = "Unknown";
    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(1);

    private readonly IMessageConsumer<SignalDecisionCommand> _commandConsumer;
    private readonly ISignalDecisionCommandRepository _commandRepository;
    private readonly IExperimentRunRepository _experimentRunRepository;
    private readonly ILogger<SignalControllerWorker> _logger;
    private readonly IMessageConsumer<TrafficMeasurement> _measurementConsumer;
    private readonly ITrafficMeasurementRepository _measurementRepository;
    private readonly SimulationSettings _simulationSettings;
    private readonly IMessagePublisher<SignalStateSnapshot> _snapshotPublisher;
    private readonly ISignalStateSnapshotRepository _snapshotRepository;
    private readonly ISignalStateStore _stateStore;

    public SignalControllerWorker(
        ILogger<SignalControllerWorker> logger,
        IMessageConsumer<SignalDecisionCommand> commandConsumer,
        IMessageConsumer<TrafficMeasurement> measurementConsumer,
        IMessagePublisher<SignalStateSnapshot> snapshotPublisher,
        ISignalStateStore stateStore,
        ISignalDecisionCommandRepository commandRepository,
        ITrafficMeasurementRepository measurementRepository,
        ISignalStateSnapshotRepository snapshotRepository,
        IExperimentRunRepository experimentRunRepository,
        SimulationSettings simulationSettings)
    {
        _logger = logger;
        _commandConsumer = commandConsumer;
        _measurementConsumer = measurementConsumer;
        _snapshotPublisher = snapshotPublisher;
        _stateStore = stateStore;
        _commandRepository = commandRepository;
        _measurementRepository = measurementRepository;
        _snapshotRepository = snapshotRepository;
        _experimentRunRepository = experimentRunRepository;
        _simulationSettings = simulationSettings;
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
                "Received command: intersection={IntersectionId} selectedPhase={SelectedPhaseId} selectedSignals={SelectedSignalIds} policy={Policy} green={GreenDurationSeconds} yellow={YellowDurationSeconds}",
                command.IntersectionId,
                command.SelectedPhaseId ?? command.SelectedSignalId,
                FormatSelectedSignalIds(command),
                command.Policy,
                command.GreenDurationSeconds,
                command.YellowDurationSeconds);

            await PersistCommandAsync(command, stoppingToken);

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
                "Applied state: intersection={IntersectionId} green={GreenSignalIds}",
                snapshot.IntersectionId,
                GetGreenSignalIds(snapshot));

            await PersistSnapshotAsync(snapshot, stoppingToken);
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

            await PersistMeasurementAsync(measurement, stoppingToken);

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

                await PersistSnapshotAsync(snapshot, stoppingToken);
                await _snapshotPublisher.PublishAsync(snapshot, stoppingToken);
            }
        }
    }

    private async Task PersistMeasurementAsync(
        TrafficMeasurement measurement,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureExperimentRunStartedAsync(
                measurement.RunId,
                UnknownPolicy,
                measurement.MeasuredAtUtc,
                cancellationToken);

            await _measurementRepository.AddAsync(measurement, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist TrafficMeasurement: messageId={MessageId} intersection={IntersectionId} signal={SignalId}",
                measurement.MessageId,
                measurement.IntersectionId,
                measurement.SignalId);
        }
    }

    private async Task PersistCommandAsync(
        SignalDecisionCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureExperimentRunStartedAsync(
                command.RunId,
                command.Policy.ToString(),
                command.IssuedAtUtc,
                cancellationToken);

            await _commandRepository.AddAsync(command, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist SignalDecisionCommand: messageId={MessageId} intersection={IntersectionId} selectedSignal={SelectedSignalId}",
                command.MessageId,
                command.IntersectionId,
                command.SelectedSignalId);
        }
    }

    private async Task PersistSnapshotAsync(
        SignalStateSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureExperimentRunStartedAsync(
                snapshot.RunId,
                UnknownPolicy,
                snapshot.CapturedAtUtc,
                cancellationToken);

            await _snapshotRepository.AddAsync(snapshot, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist SignalStateSnapshot: messageId={MessageId} intersection={IntersectionId}",
                snapshot.MessageId,
                snapshot.IntersectionId);
        }
    }

    private async Task EnsureExperimentRunStartedAsync(
        Guid runId,
        string policy,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken)
    {
        if (runId == Guid.Empty)
        {
            return;
        }

        try
        {
            await _experimentRunRepository.EnsureStartedAsync(
                runId,
                policy,
                _simulationSettings.Scenario,
                startedAtUtc,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to ensure ExperimentRun: runId={RunId} policy={Policy} scenario={Scenario}",
                runId,
                policy,
                _simulationSettings.Scenario);
        }
    }

    private static string GetGreenSignalIds(SignalStateSnapshot snapshot)
    {
        var greenSignalIds = snapshot.Signals
            .Where(signal => signal.State == SignalState.Green)
            .Select(signal => signal.SignalId)
            .ToArray();

        return greenSignalIds.Length > 0
            ? string.Join(",", greenSignalIds)
            : "none";
    }

    private static string FormatSelectedSignalIds(SignalDecisionCommand command)
    {
        return command.SelectedSignalIds.Count > 0
            ? string.Join(",", command.SelectedSignalIds)
            : command.SelectedSignalId;
    }

    private static string FormatStates(SignalStateSnapshot snapshot)
    {
        return string.Join(
            ",",
            snapshot.Signals.Select(signal => $"{signal.SignalId}={signal.State}"));
    }
}
