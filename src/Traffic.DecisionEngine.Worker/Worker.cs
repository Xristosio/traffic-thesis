using Traffic.Application.Measurements;
using Traffic.Application.Messaging;
using Traffic.Application.Policies;
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
            () => PublishFixedTimeCommandsAsync(stoppingToken),
            stoppingToken);

        await Task.WhenAll(consumeTask, commandTask);
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

    private async Task PublishFixedTimeCommandsAsync(CancellationToken stoppingToken)
    {
        if (!_scheduler.IsFixedTimeMode)
        {
            _logger.LogInformation(
                "Only FixedTime policy is currently implemented. Configured policy mode={PolicyMode}; command publishing is disabled.",
                _scheduler.ConfiguredPolicyMode);
            return;
        }

        _logger.LogInformation(
            "FixedTime command publishing enabled. Phase duration={PhaseDurationSeconds} seconds.",
            _scheduler.PhaseDuration.TotalSeconds);

        using var timer = new PeriodicTimer(CommandCheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            var commands = _scheduler.GetDueCommands(DateTimeOffset.UtcNow);

            foreach (var command in commands)
            {
                await _commandPublisher.PublishAsync(command, stoppingToken);
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
