using Traffic.Application.Measurements;
using Traffic.Application.Messaging;
using Traffic.Application.Policies;
using Traffic.Contracts.Enums;
using Traffic.Contracts.Messages;

namespace Traffic.DecisionEngine.Worker;

public class Worker : BackgroundService
{
    private static readonly TimeSpan CommandCheckInterval = TimeSpan.FromSeconds(1);

    private readonly IMessageConsumer<TrafficMeasurement> _consumer;
    private readonly IMessagePublisher<SignalDecisionCommand> _commandPublisher;
    private readonly ILogger<Worker> _logger;
    private readonly ISignalDecisionCommandScheduler _scheduler;
    private readonly ILatestMeasurementStore _store;

    public Worker(
        ILogger<Worker> logger,
        IMessageConsumer<TrafficMeasurement> consumer,
        IMessagePublisher<SignalDecisionCommand> commandPublisher,
        ISignalDecisionCommandScheduler scheduler,
        ILatestMeasurementStore store)
    {
        _logger = logger;
        _consumer = consumer;
        _commandPublisher = commandPublisher;
        _scheduler = scheduler;
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumeTask = Task.Run(
            () => ConsumeMeasurementsAsync(stoppingToken),
            stoppingToken);
        var commandTask = Task.Run(
            () => PublishCommandsAsync(stoppingToken),
            stoppingToken);

        try
        {
            await Task.WhenAll(consumeTask, commandTask);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during host shutdown.
        }
    }

    private async Task ConsumeMeasurementsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var measurement = await _consumer.ConsumeAsync(stoppingToken);
            if (measurement is null)
            {
                continue;
            }

            _store.Update(measurement);

            _logger.LogInformation(
                "Consumed measurement: intersection={IntersectionId} signal={SignalId} queue={QueueLength} arrivals={Arrivals} departures={Departures}",
                measurement.IntersectionId,
                measurement.SignalId,
                measurement.QueueLength,
                measurement.Arrivals,
                measurement.Departures);
        }
    }

    private async Task PublishCommandsAsync(CancellationToken stoppingToken)
    {
        if (_scheduler.ActivePolicy is null)
        {
            _logger.LogInformation(
                "{SchedulerDescription}",
                _scheduler.Description);
            return;
        }

        _logger.LogInformation(
            "{SchedulerDescription}",
            _scheduler.Description);

        using var timer = new PeriodicTimer(CommandCheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            var scheduledCommands = _scheduler.GetDueCommands(DateTimeOffset.UtcNow);

            foreach (var scheduledCommand in scheduledCommands)
            {
                LogScheduledCommand(scheduledCommand);

                await _commandPublisher.PublishAsync(scheduledCommand.Command, stoppingToken);
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private void LogScheduledCommand(ScheduledSignalDecisionCommand scheduledCommand)
    {
        var command = scheduledCommand.Command;

        if (command.Policy is not PolicyMode.LongestQueueFirst)
        {
            return;
        }

        if (scheduledCommand.IsFairnessSwitch)
        {
            _logger.LogInformation(
                "LQF fairness switch: intersection={IntersectionId} from={PreviousSignalId} to={SelectedSignalId} reason={Reason}",
                command.IntersectionId,
                scheduledCommand.PreviousSignalId ?? "none",
                command.SelectedSignalId,
                scheduledCommand.Reason);
            return;
        }

        _logger.LogInformation(
            "LQF decision: intersection={IntersectionId} selectedSignal={SelectedSignalId} queue={QueueLength} reason={Reason}",
            command.IntersectionId,
            command.SelectedSignalId,
            scheduledCommand.SelectedQueueLength,
            scheduledCommand.Reason);
    }
}
