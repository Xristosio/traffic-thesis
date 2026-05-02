using Traffic.Contracts.Messages;

namespace Traffic.Application.SignalStates;

public interface ISignalStateStore
{
    SignalStateSnapshot Apply(SignalDecisionCommand command, DateTimeOffset utcNow);

    IReadOnlyList<SignalStateSnapshot> GetSnapshots(DateTimeOffset utcNow);

    IReadOnlyList<SignalStateSnapshot> GetCurrentSnapshots(DateTimeOffset utcNow);

    bool TryGetCurrentSnapshot(
        string intersectionId,
        DateTimeOffset utcNow,
        out SignalStateSnapshot snapshot);
}
