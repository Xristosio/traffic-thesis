using Microsoft.EntityFrameworkCore;
using Traffic.Application.Persistence;
using Traffic.Contracts.Messages;
using Traffic.Infrastructure.Persistence.Entities;

namespace Traffic.Infrastructure.Persistence.Repositories;

public sealed class SignalDecisionCommandRepository(IDbContextFactory<TrafficDbContext> dbContextFactory)
    : ISignalDecisionCommandRepository
{
    public async Task AddAsync(
        SignalDecisionCommand command,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.SignalDecisionCommands.Add(new SignalDecisionCommandEntity
        {
            Id = Guid.NewGuid(),
            RunId = command.RunId,
            MessageId = command.MessageId,
            IntersectionId = command.IntersectionId,
            SelectedSignalId = command.SelectedSignalId,
            Policy = command.Policy.ToString(),
            GreenDurationSeconds = command.GreenDurationSeconds,
            YellowDurationSeconds = command.YellowDurationSeconds,
            IssuedAtUtc = command.IssuedAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
