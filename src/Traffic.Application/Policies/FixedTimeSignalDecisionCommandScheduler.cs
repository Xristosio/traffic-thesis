using Traffic.Application.Measurements;
using Traffic.Contracts.Configuration;
using Traffic.Contracts.Enums;
using Traffic.Contracts.Messages;
using Traffic.Domain.Topology;

namespace Traffic.Application.Policies;

public sealed class FixedTimeSignalDecisionCommandScheduler : ISignalDecisionCommandScheduler
{
    private const int SchemaVersion = 1;

    private readonly PolicySettings _settings;
    private readonly Dictionary<string, IntersectionCommandState> _states;
    private readonly ILatestMeasurementStore _store;
    private readonly TrafficTopology _topology;

    public FixedTimeSignalDecisionCommandScheduler(
        TrafficTopology topology,
        PolicySettings settings,
        ILatestMeasurementStore store)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(store);

        _topology = topology;
        _settings = settings;
        _store = store;
        _states = topology.Intersections.ToDictionary(
            intersection => intersection.Id,
            _ => new IntersectionCommandState(),
            StringComparer.OrdinalIgnoreCase);

        ValidateFixedTimeSettings(settings.FixedTime);
    }

    public PolicyMode? ActivePolicy => PolicyMode.FixedTime;

    public string ConfiguredPolicyMode => _settings.Mode;

    public string Description =>
        $"FixedTime command publishing enabled. Phase duration={PhaseDuration.TotalSeconds} seconds.";

    private TimeSpan PhaseDuration => TimeSpan.FromSeconds(
        _settings.FixedTime.GreenSeconds + _settings.FixedTime.YellowSeconds);

    public IReadOnlyList<ScheduledSignalDecisionCommand> GetDueCommands(DateTimeOffset utcNow)
    {
        var commands = new List<ScheduledSignalDecisionCommand>(_topology.Intersections.Count);

        foreach (var intersection in _topology.Intersections)
        {
            if (!_store.TryGetLatestForIntersection(intersection.Id, out var latestMeasurement))
            {
                continue;
            }

            var state = _states[intersection.Id];
            if (!state.IsDue(utcNow, PhaseDuration))
            {
                continue;
            }

            var selectedPhase = intersection.Phases[state.CurrentPhaseIndex];
            var selectedSignalId = selectedPhase.GreenSignalIds[0];

            commands.Add(new ScheduledSignalDecisionCommand(
                new SignalDecisionCommand(
                    Guid.NewGuid(),
                    latestMeasurement.RunId,
                    SchemaVersion,
                    intersection.Id,
                    selectedSignalId,
                    PolicyMode.FixedTime,
                    _settings.FixedTime.GreenSeconds,
                    _settings.FixedTime.YellowSeconds,
                    utcNow)
                {
                    SelectedPhaseId = selectedPhase.Id,
                    SelectedSignalIds = selectedPhase.GreenSignalIds.ToArray()
                },
                "fixed-cycle",
                latestMeasurement.QueueLength));

            state.MarkIssued(utcNow, intersection.Phases.Count);
        }

        return commands;
    }

    private static void ValidateFixedTimeSettings(FixedTimePolicySettings settings)
    {
        if (settings.GreenSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Policy FixedTime GreenSeconds must be greater than zero.");
        }

        if (settings.YellowSeconds < 0)
        {
            throw new InvalidOperationException(
                "Policy FixedTime YellowSeconds cannot be negative.");
        }
    }

    private sealed class IntersectionCommandState
    {
        public int CurrentPhaseIndex { get; private set; }

        public DateTimeOffset? LastCommandIssuedAtUtc { get; private set; }

        public int NextPhaseIndex => CurrentPhaseIndex;

        public bool IsDue(DateTimeOffset utcNow, TimeSpan phaseDuration)
        {
            return LastCommandIssuedAtUtc is null ||
                utcNow - LastCommandIssuedAtUtc.Value >= phaseDuration;
        }

        public void MarkIssued(DateTimeOffset issuedAtUtc, int phaseCount)
        {
            LastCommandIssuedAtUtc = issuedAtUtc;
            CurrentPhaseIndex = (NextPhaseIndex + 1) % phaseCount;
        }
    }
}
