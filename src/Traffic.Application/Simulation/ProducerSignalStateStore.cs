using System.Collections.Concurrent;
using Traffic.Contracts.Enums;
using Traffic.Contracts.Messages;

namespace Traffic.Application.Simulation;

public sealed class ProducerSignalStateStore : IProducerSignalStateStore
{
    private readonly ConcurrentDictionary<string, SignalState> _states =
        new(StringComparer.OrdinalIgnoreCase);

    public SignalState GetSignalState(string intersectionId, string signalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(intersectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(signalId);

        return _states.TryGetValue(GetKey(intersectionId, signalId), out var state)
            ? state
            : SignalState.Red;
    }

    public void Update(SignalStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        foreach (var signal in snapshot.Signals)
        {
            _states[GetKey(snapshot.IntersectionId, signal.SignalId)] = signal.State;
        }
    }

    private static string GetKey(string intersectionId, string signalId)
    {
        return $"{intersectionId}:{signalId}";
    }
}
