using Traffic.Application.Measurements;
using Traffic.Contracts.Configuration;
using Traffic.Contracts.Enums;
using Traffic.Contracts.Messages;
using Traffic.Domain.Topology;

namespace Traffic.Application.Policies;

public sealed class LongestQueueFirstSignalDecisionCommandScheduler : ISignalDecisionCommandScheduler
{
    private const int SchemaVersion = 1;

    private readonly LongestQueueFirstPolicySettings _settings;
    private readonly ILatestMeasurementStore _store;
    private readonly Dictionary<string, IntersectionCommandState> _states;
    private readonly TrafficTopology _topology;

    public LongestQueueFirstSignalDecisionCommandScheduler(
        TrafficTopology topology,
        PolicySettings settings,
        ILatestMeasurementStore store)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(store);

        ValidateSettings(settings.LongestQueueFirst);

        _topology = topology;
        _settings = settings.LongestQueueFirst;
        _store = store;
        ConfiguredPolicyMode = settings.Mode;
        _states = topology.Intersections.ToDictionary(
            intersection => intersection.Id,
            _ => new IntersectionCommandState(),
            StringComparer.OrdinalIgnoreCase);
    }

    public PolicyMode? ActivePolicy => PolicyMode.LongestQueueFirst;

    public string ConfiguredPolicyMode { get; }

    public string Description =>
        "LongestQueueFirst command publishing enabled. " +
        $"MinGreen={_settings.MinGreenSeconds}s, MaxGreen={_settings.MaxGreenSeconds}s, " +
        $"Yellow={_settings.YellowSeconds}s.";

    public IReadOnlyList<ScheduledSignalDecisionCommand> GetDueCommands(DateTimeOffset utcNow)
    {
        var commands = new List<ScheduledSignalDecisionCommand>(_topology.Intersections.Count);

        foreach (var intersection in _topology.Intersections)
        {
            var measurements = _store.GetLatestForIntersection(intersection.Id);
            if (measurements.Count == 0)
            {
                continue;
            }

            var latestMeasurement = measurements
                .OrderByDescending(measurement => measurement.MeasuredAtUtc)
                .First();
            var phaseQueues = BuildPhaseQueues(intersection, measurements);
            var state = _states[intersection.Id];
            var selection = SelectPhase(state, phaseQueues, utcNow);
            if (selection is null)
            {
                continue;
            }

            commands.Add(new ScheduledSignalDecisionCommand(
                new SignalDecisionCommand(
                    Guid.NewGuid(),
                    latestMeasurement.RunId,
                    SchemaVersion,
                    intersection.Id,
                    selection.PrimarySignalId,
                    PolicyMode.LongestQueueFirst,
                    _settings.MaxGreenSeconds,
                    _settings.YellowSeconds,
                    utcNow)
                {
                    SelectedPhaseId = selection.PhaseId,
                    SelectedSignalIds = selection.GreenSignalIds.ToArray()
                },
                selection.Reason,
                selection.Score,
                PreviousSignalId: null,
                IsFairnessSwitch: selection.IsFairnessSwitch,
                PreviousPhaseId: state.CurrentPhaseId));

            state.MarkIssued(selection.PhaseId, utcNow);
        }

        return commands;
    }

    private static IReadOnlyList<PhaseQueueInfo> BuildPhaseQueues(
        IntersectionDefinition intersection,
        IReadOnlyCollection<TrafficMeasurement> measurements)
    {
        var measurementsBySignal = measurements.ToDictionary(
            measurement => measurement.SignalId,
            StringComparer.OrdinalIgnoreCase);

        return intersection.Phases
            .Select((phase, index) =>
            {
                var score = phase.GreenSignalIds.Sum(signalId =>
                    measurementsBySignal.TryGetValue(signalId, out var measurement)
                        ? measurement.QueueLength
                        : 0);

                return new PhaseQueueInfo(
                    phase.Id,
                    phase.GreenSignalIds[0],
                    phase.GreenSignalIds,
                    score,
                    index);
            })
            .ToArray();
    }

    private PhaseSelection? SelectPhase(
        IntersectionCommandState state,
        IReadOnlyList<PhaseQueueInfo> phaseQueues,
        DateTimeOffset utcNow)
    {
        var largestQueue = GetLargestQueue(phaseQueues);
        if (state.LastCommandIssuedAtUtc is null)
        {
            return new PhaseSelection(
                largestQueue.PhaseId,
                largestQueue.PrimarySignalId,
                largestQueue.GreenSignalIds,
                largestQueue.Score,
                "largest-queue",
                IsFairnessSwitch: false);
        }

        var elapsed = utcNow - state.LastCommandIssuedAtUtc.Value;
        if (elapsed < TimeSpan.FromSeconds(_settings.MinGreenSeconds))
        {
            return null;
        }

        var currentPhaseId = state.CurrentPhaseId;
        var bestAlternative = GetBestAlternativeMeetingThreshold(phaseQueues, currentPhaseId);

        if (state.ConsecutiveSelections >= _settings.MaxConsecutiveSelections &&
            bestAlternative is not null)
        {
            return new PhaseSelection(
                bestAlternative.PhaseId,
                bestAlternative.PrimarySignalId,
                bestAlternative.GreenSignalIds,
                bestAlternative.Score,
                "max-consecutive",
                IsFairnessSwitch: true);
        }

        if (string.Equals(largestQueue.PhaseId, currentPhaseId, StringComparison.OrdinalIgnoreCase) &&
            elapsed < TimeSpan.FromSeconds(_settings.MaxGreenSeconds))
        {
            return null;
        }

        if (elapsed >= TimeSpan.FromSeconds(_settings.MaxGreenSeconds) &&
            string.Equals(largestQueue.PhaseId, currentPhaseId, StringComparison.OrdinalIgnoreCase) &&
            bestAlternative is not null)
        {
            return new PhaseSelection(
                bestAlternative.PhaseId,
                bestAlternative.PrimarySignalId,
                bestAlternative.GreenSignalIds,
                bestAlternative.Score,
                "max-green",
                IsFairnessSwitch: true);
        }

        return new PhaseSelection(
            largestQueue.PhaseId,
            largestQueue.PrimarySignalId,
            largestQueue.GreenSignalIds,
            largestQueue.Score,
            "largest-queue",
            IsFairnessSwitch: false);
    }

    private static PhaseQueueInfo GetLargestQueue(IReadOnlyList<PhaseQueueInfo> phaseQueues)
    {
        return phaseQueues
            .OrderByDescending(phase => phase.Score)
            .ThenBy(phase => phase.TopologyOrder)
            .First();
    }

    private PhaseQueueInfo? GetBestAlternativeMeetingThreshold(
        IReadOnlyList<PhaseQueueInfo> phaseQueues,
        string? currentPhaseId)
    {
        if (currentPhaseId is null)
        {
            return null;
        }

        return phaseQueues
            .Where(phase =>
                !string.Equals(phase.PhaseId, currentPhaseId, StringComparison.OrdinalIgnoreCase) &&
                phase.Score >= _settings.FairnessQueueThreshold)
            .OrderByDescending(phase => phase.Score)
            .ThenBy(phase => phase.TopologyOrder)
            .FirstOrDefault();
    }

    private static void ValidateSettings(LongestQueueFirstPolicySettings settings)
    {
        if (settings.MinGreenSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Policy LongestQueueFirst MinGreenSeconds must be greater than zero.");
        }

        if (settings.MaxGreenSeconds < settings.MinGreenSeconds)
        {
            throw new InvalidOperationException(
                "Policy LongestQueueFirst MaxGreenSeconds must be greater than or equal to MinGreenSeconds.");
        }

        if (settings.YellowSeconds < 0)
        {
            throw new InvalidOperationException(
                "Policy LongestQueueFirst YellowSeconds cannot be negative.");
        }

        if (settings.MaxConsecutiveSelections <= 0)
        {
            throw new InvalidOperationException(
                "Policy LongestQueueFirst MaxConsecutiveSelections must be greater than zero.");
        }

        if (settings.FairnessQueueThreshold < 0)
        {
            throw new InvalidOperationException(
                "Policy LongestQueueFirst FairnessQueueThreshold cannot be negative.");
        }
    }

    private sealed class IntersectionCommandState
    {
        public string? CurrentPhaseId { get; private set; }

        public DateTimeOffset? LastCommandIssuedAtUtc { get; private set; }

        public int ConsecutiveSelections { get; private set; }

        public void MarkIssued(string selectedPhaseId, DateTimeOffset issuedAtUtc)
        {
            ConsecutiveSelections = string.Equals(
                CurrentPhaseId,
                selectedPhaseId,
                StringComparison.OrdinalIgnoreCase)
                ? ConsecutiveSelections + 1
                : 1;

            CurrentPhaseId = selectedPhaseId;
            LastCommandIssuedAtUtc = issuedAtUtc;
        }
    }

    private sealed record PhaseQueueInfo(
        string PhaseId,
        string PrimarySignalId,
        IReadOnlyList<string> GreenSignalIds,
        int Score,
        int TopologyOrder);

    private sealed record PhaseSelection(
        string PhaseId,
        string PrimarySignalId,
        IReadOnlyList<string> GreenSignalIds,
        int Score,
        string Reason,
        bool IsFairnessSwitch);
}
