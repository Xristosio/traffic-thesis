using Traffic.Application.Simulation;
using Traffic.Contracts.Configuration;

namespace Traffic.Producer.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IProducerSimulationService _simulation;
    private readonly SimulationSettings _settings;

    public Worker(
        ILogger<Worker> logger,
        IProducerSimulationService simulation,
        SimulationSettings settings)
    {
        _logger = logger;
        _simulation = simulation;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var measurements = _simulation.GenerateMeasurements();

            foreach (var measurement in measurements)
            {
                _logger.LogInformation(
                    "Generated measurement: run={RunId} intersection={IntersectionId} signal={SignalId} queue={QueueLength} arrivals={Arrivals} departures={Departures}",
                    measurement.RunId,
                    measurement.IntersectionId,
                    measurement.SignalId,
                    measurement.QueueLength,
                    measurement.Arrivals,
                    measurement.Departures);
            }

            await Task.Delay(_settings.TickMilliseconds, stoppingToken);
        }
    }
}
