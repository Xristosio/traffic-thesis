using Traffic.Contracts.Configuration;
using Traffic.Contracts.Messages;
using Traffic.Domain.Simulation;
using Traffic.Domain.Topology;

namespace Traffic.Application.Simulation;

public sealed class ProducerSimulationService : IProducerSimulationService
{
    private const int SchemaVersion = 1;
    private const string BalancedScenario = "Balanced";

    private readonly Random _random;
    private readonly SimulationSettings _settings;
    private readonly IReadOnlyList<SignalQueueState> _signalQueues;

    public ProducerSimulationService(TrafficTopology topology, SimulationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(settings);

        ValidateSettings(settings);

        _settings = settings;
        _random = settings.RandomSeed.HasValue
            ? new Random(settings.RandomSeed.Value)
            : new Random();
        _signalQueues = topology.Intersections
            .SelectMany(intersection => intersection.Signals.Select(signal =>
                new SignalQueueState(intersection.Id, signal.Id)))
            .ToArray();
    }

    public Guid RunId { get; } = Guid.NewGuid();

    public IReadOnlyList<TrafficMeasurement> GenerateMeasurements()
    {
        var measurements = new List<TrafficMeasurement>(_signalQueues.Count);

        foreach (var queue in _signalQueues)
        {
            var arrivals = GenerateArrivals();
            const int departures = 0;

            queue.ApplyMeasurementDelta(arrivals, departures);

            measurements.Add(new TrafficMeasurement(
                Guid.NewGuid(),
                RunId,
                SchemaVersion,
                queue.IntersectionId,
                queue.SignalId,
                queue.CurrentQueueLength,
                arrivals,
                departures,
                DateTimeOffset.UtcNow));
        }

        return measurements;
    }

    private int GenerateArrivals()
    {
        return _random.Next(0, 4);
    }

    private static void ValidateSettings(SimulationSettings settings)
    {
        if (settings.TickMilliseconds <= 0)
        {
            throw new InvalidOperationException(
                "Simulation TickMilliseconds must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(settings.Scenario))
        {
            throw new InvalidOperationException("Simulation Scenario must be configured.");
        }

        if (!settings.Scenario.Trim().Equals(BalancedScenario, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Simulation scenario '{settings.Scenario}' is not supported.");
        }
    }
}
