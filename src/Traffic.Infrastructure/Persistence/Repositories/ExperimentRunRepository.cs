using Microsoft.EntityFrameworkCore;
using Npgsql;
using Traffic.Application.Persistence;
using Traffic.Infrastructure.Persistence.Entities;

namespace Traffic.Infrastructure.Persistence.Repositories;

public sealed class ExperimentRunRepository(IDbContextFactory<TrafficDbContext> dbContextFactory)
    : IExperimentRunRepository
{
    private const string Unknown = "Unknown";

    public async Task<ExperimentRun?> GetByIdAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await dbContext.ExperimentRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(run => run.Id == runId, cancellationToken);

        return entity is null
            ? null
            : ToModel(entity);
    }

    public async Task EnsureStartedAsync(
        Guid runId,
        string policy,
        string scenario,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken)
    {
        if (runId == Guid.Empty)
        {
            throw new ArgumentException("RunId must not be empty.", nameof(runId));
        }

        var normalizedPolicy = Normalize(policy);
        var normalizedScenario = Normalize(scenario);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.ExperimentRuns
            .FirstOrDefaultAsync(run => run.Id == runId, cancellationToken);

        if (existing is not null)
        {
            if (ApplyStartedUpdates(existing, normalizedPolicy, normalizedScenario, startedAtUtc))
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        dbContext.ExperimentRuns.Add(new ExperimentRunEntity
        {
            Id = runId,
            Policy = normalizedPolicy,
            Scenario = normalizedScenario,
            StartedAtUtc = startedAtUtc
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await UpdateExistingAfterConcurrentInsertAsync(
                runId,
                normalizedPolicy,
                normalizedScenario,
                startedAtUtc,
                cancellationToken);
        }
    }

    public async Task MarkFinishedAsync(
        Guid runId,
        DateTimeOffset finishedAtUtc,
        CancellationToken cancellationToken)
    {
        if (runId == Guid.Empty)
        {
            throw new ArgumentException("RunId must not be empty.", nameof(runId));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.ExperimentRuns
            .FirstOrDefaultAsync(run => run.Id == runId, cancellationToken);

        if (existing is null)
        {
            return;
        }

        if (existing.FinishedAtUtc is null || existing.FinishedAtUtc < finishedAtUtc)
        {
            existing.FinishedAtUtc = finishedAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpdateExistingAfterConcurrentInsertAsync(
        Guid runId,
        string policy,
        string scenario,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.ExperimentRuns
            .FirstOrDefaultAsync(run => run.Id == runId, cancellationToken);

        if (existing is null)
        {
            return;
        }

        if (ApplyStartedUpdates(existing, policy, scenario, startedAtUtc))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool ApplyStartedUpdates(
        ExperimentRunEntity existing,
        string policy,
        string scenario,
        DateTimeOffset startedAtUtc)
    {
        var changed = false;

        if (existing.StartedAtUtc > startedAtUtc)
        {
            existing.StartedAtUtc = startedAtUtc;
            changed = true;
        }

        if (IsUnknown(existing.Policy) && !IsUnknown(policy))
        {
            existing.Policy = policy;
            changed = true;
        }

        if (IsUnknown(existing.Scenario) && !IsUnknown(scenario))
        {
            existing.Scenario = scenario;
            changed = true;
        }

        return changed;
    }

    private static ExperimentRun ToModel(ExperimentRunEntity entity)
    {
        return new ExperimentRun(
            entity.Id,
            entity.Policy,
            entity.Scenario,
            entity.StartedAtUtc,
            entity.FinishedAtUtc);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Unknown
            : value.Trim();
    }

    private static bool IsUnknown(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            || string.Equals(value, Unknown, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
    }
}
