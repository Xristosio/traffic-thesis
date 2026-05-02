using Traffic.Application.Measurements;
using Traffic.Application.Messaging;
using Traffic.Contracts.Messages;

namespace Traffic.DecisionEngine.Worker;

public class Worker : BackgroundService
{
    private readonly IMessageConsumer<TrafficMeasurement> _consumer;
    private readonly ILogger<Worker> _logger;
    private readonly ILatestMeasurementStore _store;

    public Worker(
        ILogger<Worker> logger,
        IMessageConsumer<TrafficMeasurement> consumer,
        ILatestMeasurementStore store)
    {
        _logger = logger;
        _consumer = consumer;
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
}
