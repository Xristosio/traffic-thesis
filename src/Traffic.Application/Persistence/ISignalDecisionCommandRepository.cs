using Traffic.Contracts.Messages;

namespace Traffic.Application.Persistence;

public interface ISignalDecisionCommandRepository
{
    Task AddAsync(SignalDecisionCommand command, CancellationToken cancellationToken);
}
