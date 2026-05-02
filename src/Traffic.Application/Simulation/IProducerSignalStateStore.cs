using Traffic.Contracts.Enums;
using Traffic.Contracts.Messages;

namespace Traffic.Application.Simulation;

public interface IProducerSignalStateStore
{
    SignalState GetSignalState(string intersectionId, string signalId);

    void Update(SignalStateSnapshot snapshot);
}
