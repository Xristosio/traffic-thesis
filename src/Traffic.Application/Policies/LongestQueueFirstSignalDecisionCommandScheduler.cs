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
            var signalQueues = BuildSignalQueues(intersection, measurements);
            var state = _states[intersection.Id];
            var selection = SelectSignal(state, signalQueues, utcNow);
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
                    selection.SignalId,
                    PolicyMode.LongestQueueFirst,
                    _settings.MaxGreenSeconds,
                    _settings.YellowSeconds,
                    utcNow),
                selection.Reason,
                selection.QueueLength,
                state.CurrentSignalId,
                selection.IsFairnessSwitch));

            state.MarkIssued(selection.SignalId, utcNow);
        }

        return commands;
    }

    private IReadOnlyList<SignalQueueInfo> BuildSignalQueues(
        IntersectionDefinition intersection,
        IReadOnlyCollection<TrafficMeasurement> measurements)
    {
        var measurementsBySignal = measurements.ToDictionary(
            measurement => measurement.SignalId,
            StringComparer.OrdinalIgnoreCase);

        return intersection.Signals
            .Select((signal, index) =>
            {
                return measurementsBySignal.TryGetValue(signal.Id, out var measurement)
                    ? new SignalQueueInfo(signal.Id, measurement.QueueLength, index)
                    : new SignalQueueInfo(signal.Id, 0, index);
            })
            .ToArray();
    }

    private SignalSelection? SelectSignal(
        IntersectionCommandState state,
        IReadOnlyList<SignalQueueInfo> signalQueues,
        DateTimeOffset utcNow)
    {
        var largestQueue = GetLargestQueue(signalQueues);
        if (state.LastCommandIssuedAtUtc is null)
        {
            return new SignalSelection(
                largestQueue.SignalId,
                largestQueue.QueueLength,
                "largest-queue",
                IsFairnessSwitch: false);
        }

        var elapsed = utcNow - state.LastCommandIssuedAtUtc.Value;
        if (elapsed < TimeSpan.FromSeconds(_settings.MinGreenSeconds))
        {
            return null;
        }

        var currentSignalId = state.CurrentSignalId;
        var bestAlternative = GetBestAlternativeMeetingThreshold(signalQueues, currentSignalId);

        if (state.ConsecutiveSelections >= _settings.MaxConsecutiveSelections &&
            bestAlternative is not null)
        {
            return new SignalSelection(
                bestAlternative.SignalId,
                bestAlternative.QueueLength,
                "max-consecutive",
                IsFairnessSwitch: true);
        }

        if (string.Equals(largestQueue.SignalId, currentSignalId, StringComparison.OrdinalIgnoreCase) &&
            elapsed < TimeSpan.FromSeconds(_settings.MaxGreenSeconds))
        {
            return null;
        }

        if (elapsed >= TimeSpan.FromSeconds(_settings.MaxGreenSeconds) &&
            string.Equals(largestQueue.SignalId, currentSignalId, StringComparison.OrdinalIgnoreCase) &&
            bestAlternative is not null)
        {
            return new SignalSelection(
                bestAlternative.SignalId,
                bestAlternative.QueueLength,
                "max-green",
                IsFairnessSwitch: true);
        }

        return new SignalSelection(
            largestQueue.SignalId,
            largestQueue.QueueLength,
            "largest-queue",
            IsFairnessSwitch: false);
    }

    private SignalQueueInfo GetLargestQueue(IReadOnlyList<SignalQueueInfo> signalQueues)
    {
        return signalQueues
            .OrderByDescending(signal => signal.QueueLength)
            .ThenBy(signal => signal.TopologyOrder)
            .First();
    }

    private SignalQueueInfo? GetBestAlternativeMeetingThreshold(
        IReadOnlyList<SignalQueueInfo> signalQueues,
        string? currentSignalId)
    {
        if (currentSignalId is null)
        {
            return null;
        }

        return signalQueues
            .Where(signal =>
                !string.Equals(signal.SignalId, currentSignalId, StringComparison.OrdinalIgnoreCase) &&
                signal.QueueLength >= _settings.FairnessQueueThreshold)
            .OrderByDescending(signal => signal.QueueLength)
            .ThenBy(signal => signal.TopologyOrder)
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
        public string? CurrentSignalId { get; private set; }

        public DateTimeOffset? LastCommandIssuedAtUtc { get; private set; }

        public int ConsecutiveSelections { get; private set; }

        public void MarkIssued(string selectedSignalId, DateTimeOffset issuedAtUtc)
        {
            ConsecutiveSelections = string.Equals(
                CurrentSignalId,
                selectedSignalId,
                StringComparison.OrdinalIgnoreCase)
                ? ConsecutiveSelections + 1
                : 1;

            CurrentSignalId = selectedSignalId;
            LastCommandIssuedAtUtc = issuedAtUtc;
        }
    }

    private sealed record SignalQueueInfo(
        string SignalId,
        int QueueLength,
        int TopologyOrder);

    private sealed record SignalSelection(
        string SignalId,
        int QueueLength,
        string Reason,
        bool IsFairnessSwitch);
}
