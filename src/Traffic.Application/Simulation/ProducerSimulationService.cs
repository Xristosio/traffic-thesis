using Traffic.Contracts.Configuration;
using Traffic.Contracts.Enums;
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
    private readonly IProducerSignalStateStore _signalStates;

    public ProducerSimulationService(
        TrafficTopology topology,
        SimulationSettings settings,
        IProducerSignalStateStore signalStates)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(signalStates);

        ValidateSettings(settings);

        _settings = settings;
        _signalStates = signalStates;
        _random = settings.RandomSeed.HasValue
            ? new Random(settings.RandomSeed.Value)
            : new Random();
        _signalQueues = topology.Intersections
            .SelectMany(intersection => intersection.Signals.Select(signal =>
                new SignalQueueState(intersection.Id, signal.Id)))
            .ToArray();
    }

    public Guid RunId { get; } = Guid.NewGuid();

    public IReadOnlyList<GeneratedTrafficMeasurement> GenerateMeasurements()
    {
        var measurements = new List<GeneratedTrafficMeasurement>(_signalQueues.Count);

        foreach (var queue in _signalQueues)
        {
            var arrivals = GenerateArrivals();
            var signalState = _signalStates.GetSignalState(queue.IntersectionId, queue.SignalId);
            var departures = CalculateDepartures(
                signalState,
                queue.CurrentQueueLength,
                arrivals);

            queue.ApplyMeasurementDelta(arrivals, departures);

            measurements.Add(new GeneratedTrafficMeasurement(
                new TrafficMeasurement(
                    Guid.NewGuid(),
                    RunId,
                    SchemaVersion,
                    queue.IntersectionId,
                    queue.SignalId,
                    queue.CurrentQueueLength,
                    arrivals,
                    departures,
                    DateTimeOffset.UtcNow),
                signalState));
        }

        return measurements;
    }

    private int GenerateArrivals()
    {
        return _random.Next(0, 4);
    }

    private int CalculateDepartures(
        SignalState signalState,
        int currentQueueLength,
        int arrivals)
    {
        if (signalState is not SignalState.Green)
        {
            return 0;
        }

        return Math.Min(currentQueueLength + arrivals, _settings.DepartureRatePerTick);
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

        if (settings.DepartureRatePerTick <= 0)
        {
            throw new InvalidOperationException(
                "Simulation DepartureRatePerTick must be greater than zero.");
        }

        if (settings.RunDurationSeconds is <= 0)
        {
            throw new InvalidOperationException(
                "Simulation RunDurationSeconds must be null or greater than zero.");
        }
    }
}
