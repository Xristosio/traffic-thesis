using Microsoft.EntityFrameworkCore;
using Traffic.Application.Persistence;
using Traffic.Contracts.Messages;
using Traffic.Infrastructure.Persistence.Entities;

namespace Traffic.Infrastructure.Persistence.Repositories;

public sealed class SignalStateSnapshotRepository(IDbContextFactory<TrafficDbContext> dbContextFactory)
    : ISignalStateSnapshotRepository
{
    public async Task AddAsync(
        SignalStateSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.SignalStateSnapshots.Add(new SignalStateSnapshotEntity
        {
            Id = Guid.NewGuid(),
            RunId = snapshot.RunId,
            MessageId = snapshot.MessageId,
            IntersectionId = snapshot.IntersectionId,
            CapturedAtUtc = snapshot.CapturedAtUtc,
            Signals = snapshot.Signals
                .Select(signal => new SignalStateItemEntity
                {
                    Id = Guid.NewGuid(),
                    SignalId = signal.SignalId,
                    State = signal.State.ToString(),
                    ElapsedSeconds = signal.ElapsedSeconds,
                    QueueLength = signal.QueueLength
                })
                .ToList()
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
