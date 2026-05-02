using Traffic.Contracts.Messages;

namespace Traffic.Application.Persistence;

public interface ISignalStateSnapshotRepository
{
    Task AddAsync(SignalStateSnapshot snapshot, CancellationToken cancellationToken);
}
