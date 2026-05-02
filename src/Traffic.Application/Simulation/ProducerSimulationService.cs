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
    private const string UnbalancedScenario = "Unbalanced";

    private readonly Random _random;
    private readonly SimulationSettings _settings;
    private readonly HashSet<string> _highTrafficSignals;
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
        _highTrafficSignals = topology.Intersections
            .Select(intersection => intersection.Signals.FirstOrDefault() is { } signal
                ? GetSignalKey(intersection.Id, signal.Id)
                : null)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
            var arrivals = GenerateArrivals(queue);
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

    private int GenerateArrivals(SignalQueueState queue)
    {
        return _settings.Scenario.Trim() switch
        {
            var scenario when scenario.Equals(BalancedScenario, StringComparison.OrdinalIgnoreCase) =>
                _random.Next(0, 4),
            var scenario when scenario.Equals(UnbalancedScenario, StringComparison.OrdinalIgnoreCase) =>
                GenerateUnbalancedArrivals(queue),
            _ => throw new NotSupportedException(
                $"Simulation scenario '{_settings.Scenario}' is not supported.")
        };
    }

    private int GenerateUnbalancedArrivals(SignalQueueState queue)
    {
        return _highTrafficSignals.Contains(GetSignalKey(queue.IntersectionId, queue.SignalId))
            ? _random.Next(2, 7)
            : _random.Next(0, 3);
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

        if (!IsSupportedScenario(settings.Scenario))
        {
            throw new NotSupportedException(
                $"Simulation scenario '{settings.Scenario}' is not supported. Supported scenarios: {BalancedScenario}, {UnbalancedScenario}.");
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

    private static bool IsSupportedScenario(string scenario)
    {
        return scenario.Trim().Equals(BalancedScenario, StringComparison.OrdinalIgnoreCase)
            || scenario.Trim().Equals(UnbalancedScenario, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSignalKey(string intersectionId, string signalId)
    {
        return $"{intersectionId}:{signalId}";
    }
}
