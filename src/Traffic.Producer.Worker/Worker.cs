using Traffic.Application.Messaging;
using Traffic.Application.Simulation;
using Traffic.Contracts.Configuration;
using Traffic.Contracts.Messages;

namespace Traffic.Producer.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMessagePublisher<TrafficMeasurement> _publisher;
    private readonly IProducerSimulationService _simulation;
    private readonly IMessageConsumer<SignalStateSnapshot> _stateConsumer;
    private readonly IProducerSignalStateStore _stateStore;
    private readonly SimulationSettings _settings;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public Worker(
        ILogger<Worker> logger,
        IMessagePublisher<TrafficMeasurement> publisher,
        IProducerSimulationService simulation,
        IMessageConsumer<SignalStateSnapshot> stateConsumer,
        IProducerSignalStateStore stateStore,
        SimulationSettings settings,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _publisher = publisher;
        _simulation = simulation;
        _stateConsumer = stateConsumer;
        _stateStore = stateStore;
        _settings = settings;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogRunStartup();

        var stateConsumeTask = Task.Run(
            () => ConsumeSignalStatesAsync(stoppingToken),
            stoppingToken);
        var generationTask = Task.Run(
            () => GenerateMeasurementsAsync(stoppingToken),
            stoppingToken);

        try
        {
            await Task.WhenAll(stateConsumeTask, generationTask);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during host shutdown.
        }
    }

    private async Task GenerateMeasurementsAsync(CancellationToken stoppingToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var endsAtUtc = _settings.RunDurationSeconds.HasValue
            ? startedAtUtc.AddSeconds(_settings.RunDurationSeconds.Value)
            : (DateTimeOffset?)null;

        while (!stoppingToken.IsCancellationRequested && ShouldContinueGenerating(endsAtUtc))
        {
            var generatedMeasurements = _simulation.GenerateMeasurements();

            foreach (var generated in generatedMeasurements)
            {
                var measurement = generated.Measurement;

                _logger.LogInformation(
                    "Generated measurement: run={RunId} intersection={IntersectionId} signal={SignalId} state={SignalState} queue={QueueLength} arrivals={Arrivals} departures={Departures}",
                    measurement.RunId,
                    measurement.IntersectionId,
                    measurement.SignalId,
                    generated.SignalState,
                    measurement.QueueLength,
                    measurement.Arrivals,
                    measurement.Departures);

                await _publisher.PublishAsync(measurement, stoppingToken);
            }

            var delay = GetNextTickDelay(endsAtUtc);
            if (delay <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(delay, stoppingToken);
        }

        if (!stoppingToken.IsCancellationRequested && endsAtUtc.HasValue)
        {
            _logger.LogInformation(
                "Producer run completed: run={RunId} durationSeconds={RunDurationSeconds} stopProducerAfterRun={StopProducerAfterRun}",
                _simulation.RunId,
                _settings.RunDurationSeconds,
                _settings.StopProducerAfterRun);

            if (_settings.StopProducerAfterRun)
            {
                _hostApplicationLifetime.StopApplication();
            }
        }
    }

    private async Task ConsumeSignalStatesAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var snapshot = await _stateConsumer.ConsumeAsync(stoppingToken);
            if (snapshot is null)
            {
                continue;
            }

            _stateStore.Update(snapshot);

            _logger.LogInformation(
                "Updated producer signal states: intersection={IntersectionId} states={States}",
                snapshot.IntersectionId,
                FormatStates(snapshot));
        }
    }

    private static string FormatStates(SignalStateSnapshot snapshot)
    {
        return string.Join(
            ",",
            snapshot.Signals.Select(signal => $"{signal.SignalId}={signal.State}"));
    }

    private void LogRunStartup()
    {
        _logger.LogInformation(
            "Producer run starting: run={RunId} scenario={Scenario} randomSeed={RandomSeed} tickMilliseconds={TickMilliseconds} runDurationSeconds={RunDurationSeconds} stopProducerAfterRun={StopProducerAfterRun} experimentName={ExperimentName}",
            _simulation.RunId,
            _settings.Scenario,
            _settings.RandomSeed,
            _settings.TickMilliseconds,
            _settings.RunDurationSeconds,
            _settings.StopProducerAfterRun,
            string.IsNullOrWhiteSpace(_settings.ExperimentName) ? "none" : _settings.ExperimentName);
    }

    private static bool ShouldContinueGenerating(DateTimeOffset? endsAtUtc)
    {
        return !endsAtUtc.HasValue || DateTimeOffset.UtcNow < endsAtUtc.Value;
    }

    private TimeSpan GetNextTickDelay(DateTimeOffset? endsAtUtc)
    {
        var tickDelay = TimeSpan.FromMilliseconds(_settings.TickMilliseconds);
        if (!endsAtUtc.HasValue)
        {
            return tickDelay;
        }

        var remaining = endsAtUtc.Value - DateTimeOffset.UtcNow;
        return remaining < tickDelay
            ? remaining
            : tickDelay;
    }
}
