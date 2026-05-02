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

    public Worker(
        ILogger<Worker> logger,
        IMessagePublisher<TrafficMeasurement> publisher,
        IProducerSimulationService simulation,
        IMessageConsumer<SignalStateSnapshot> stateConsumer,
        IProducerSignalStateStore stateStore,
        SimulationSettings settings)
    {
        _logger = logger;
        _publisher = publisher;
        _simulation = simulation;
        _stateConsumer = stateConsumer;
        _stateStore = stateStore;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        while (!stoppingToken.IsCancellationRequested)
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

            await Task.Delay(_settings.TickMilliseconds, stoppingToken);
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
}
