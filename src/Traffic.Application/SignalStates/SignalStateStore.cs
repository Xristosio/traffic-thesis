using Traffic.Contracts.Enums;
using Traffic.Contracts.Messages;
using Traffic.Domain.Topology;

namespace Traffic.Application.SignalStates;

public sealed class SignalStateStore : ISignalStateStore
{
    private const int SchemaVersion = 1;

    private readonly object _syncRoot = new();
    private readonly Dictionary<string, IntersectionSignalState> _intersections;

    public SignalStateStore(TrafficTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        _intersections = topology.Intersections.ToDictionary(
            intersection => intersection.Id,
            intersection => new IntersectionSignalState(
                intersection.Id,
                intersection.Signals
                    .Select(signal => new SignalRuntimeState(intersection.Id, signal.Id))
                    .ToArray()),
            StringComparer.OrdinalIgnoreCase);
    }

    public SignalStateSnapshot Apply(SignalDecisionCommand command, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(command);

        lock (_syncRoot)
        {
            if (!_intersections.TryGetValue(command.IntersectionId, out var intersection))
            {
                throw new InvalidOperationException(
                    $"Signal command targets unknown intersection '{command.IntersectionId}'.");
            }

            if (!intersection.Signals.Any(signal =>
                string.Equals(signal.SignalId, command.SelectedSignalId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Signal command targets unknown signal '{command.SelectedSignalId}' in intersection '{command.IntersectionId}'.");
            }

            intersection.ActiveCommand = new ActiveSignalCommand(
                command.RunId,
                command.SelectedSignalId,
                utcNow,
                TimeSpan.FromSeconds(command.GreenDurationSeconds),
                TimeSpan.FromSeconds(command.YellowDurationSeconds));

            return BuildSnapshot(intersection, utcNow);
        }
    }

    public IReadOnlyList<SignalStateSnapshot> GetSnapshots(DateTimeOffset utcNow)
    {
        lock (_syncRoot)
        {
            return _intersections.Values
                .Where(intersection => intersection.ActiveCommand is not null)
                .Select(intersection => BuildSnapshot(intersection, utcNow))
                .ToArray();
        }
    }

    public IReadOnlyList<SignalStateSnapshot> GetCurrentSnapshots(DateTimeOffset utcNow)
    {
        lock (_syncRoot)
        {
            return _intersections.Values
                .Select(intersection => BuildSnapshot(intersection, utcNow))
                .ToArray();
        }
    }

    public bool TryGetCurrentSnapshot(
        string intersectionId,
        DateTimeOffset utcNow,
        out SignalStateSnapshot snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(intersectionId);

        lock (_syncRoot)
        {
            if (!_intersections.TryGetValue(intersectionId, out var intersection))
            {
                snapshot = null!;
                return false;
            }

            snapshot = BuildSnapshot(intersection, utcNow);
            return true;
        }
    }

    private static SignalStateSnapshot BuildSnapshot(
        IntersectionSignalState intersection,
        DateTimeOffset utcNow)
    {
        var activeCommand = intersection.ActiveCommand;
        if (activeCommand is null)
        {
            return new SignalStateSnapshot(
                Guid.NewGuid(),
                Guid.Empty,
                SchemaVersion,
                intersection.IntersectionId,
                intersection.Signals
                    .Select(signal => new SignalSnapshotItem(signal.SignalId, SignalState.Red, 0, signal.QueueLength))
                    .ToArray(),
                utcNow);
        }

        foreach (var signal in intersection.Signals)
        {
            if (!string.Equals(signal.SignalId, activeCommand.SelectedSignalId, StringComparison.OrdinalIgnoreCase))
            {
                signal.Update(SignalState.Red, 0, signal.QueueLength);
                continue;
            }

            var elapsed = utcNow - activeCommand.AppliedAtUtc;
            if (elapsed < activeCommand.GreenDuration)
            {
                signal.Update(
                    SignalState.Green,
                    ToWholeSeconds(elapsed),
                    signal.QueueLength);
                continue;
            }

            if (elapsed < activeCommand.GreenDuration + activeCommand.YellowDuration)
            {
                signal.Update(
                    SignalState.Yellow,
                    ToWholeSeconds(elapsed - activeCommand.GreenDuration),
                    signal.QueueLength);
                continue;
            }

            signal.Update(
                SignalState.Red,
                ToWholeSeconds(elapsed - activeCommand.GreenDuration - activeCommand.YellowDuration),
                signal.QueueLength);
        }

        return new SignalStateSnapshot(
            Guid.NewGuid(),
            activeCommand.RunId,
            SchemaVersion,
            intersection.IntersectionId,
            intersection.Signals
                .Select(signal => new SignalSnapshotItem(
                    signal.SignalId,
                    signal.CurrentState,
                    signal.ElapsedSeconds,
                    signal.QueueLength))
                .ToArray(),
            utcNow);
    }

    private static int ToWholeSeconds(TimeSpan elapsed)
    {
        return Math.Max(0, (int)Math.Floor(elapsed.TotalSeconds));
    }

    private sealed class IntersectionSignalState
    {
        public IntersectionSignalState(
            string intersectionId,
            IReadOnlyList<SignalRuntimeState> signals)
        {
            IntersectionId = intersectionId;
            Signals = signals;
        }

        public string IntersectionId { get; }

        public IReadOnlyList<SignalRuntimeState> Signals { get; }

        public ActiveSignalCommand? ActiveCommand { get; set; }
    }

    private sealed record ActiveSignalCommand(
        Guid RunId,
        string SelectedSignalId,
        DateTimeOffset AppliedAtUtc,
        TimeSpan GreenDuration,
        TimeSpan YellowDuration);
}
