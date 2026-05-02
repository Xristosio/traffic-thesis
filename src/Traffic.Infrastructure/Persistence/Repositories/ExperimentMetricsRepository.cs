using Microsoft.EntityFrameworkCore;
using Traffic.Application.Metrics;
using Traffic.Infrastructure.Persistence.Entities;

namespace Traffic.Infrastructure.Persistence.Repositories;

public sealed class ExperimentMetricsRepository(IDbContextFactory<TrafficDbContext> dbContextFactory)
    : IExperimentMetricsRepository
{
    public async Task<IReadOnlyList<ExperimentRunListItem>> GetRunsAsync(
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.ExperimentRuns
            .AsNoTracking()
            .OrderByDescending(run => run.StartedAtUtc)
            .Select(run => new ExperimentRunListItem(
                run.Id,
                run.Policy,
                run.Scenario,
                run.StartedAtUtc,
                run.FinishedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ExperimentRunMetrics?> GetMetricsAsync(
        Guid runId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var run = await dbContext.ExperimentRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(run => run.Id == runId, cancellationToken);

        return run is null
            ? null
            : await BuildMetricsAsync(dbContext, run, cancellationToken);
    }

    public async Task<ExperimentRunMetrics?> GetLatestMetricsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var run = await dbContext.ExperimentRuns
            .AsNoTracking()
            .OrderByDescending(run => run.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return run is null
            ? null
            : await BuildMetricsAsync(dbContext, run, cancellationToken);
    }

    private static async Task<ExperimentRunMetrics> BuildMetricsAsync(
        TrafficDbContext dbContext,
        ExperimentRunEntity run,
        CancellationToken cancellationToken)
    {
        var measurementAggregate = await dbContext.TrafficMeasurements
            .AsNoTracking()
            .Where(measurement => measurement.RunId == run.Id)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.LongCount(),
                AverageQueueLength = group.Average(measurement => (double)measurement.QueueLength),
                MaxQueueLength = group.Max(measurement => measurement.QueueLength),
                TotalArrivals = group.Sum(measurement => (long)measurement.Arrivals),
                TotalDepartures = group.Sum(measurement => (long)measurement.Departures),
                LatestAtUtc = group.Max(measurement => measurement.MeasuredAtUtc)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var commandAggregate = await dbContext.SignalDecisionCommands
            .AsNoTracking()
            .Where(command => command.RunId == run.Id)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.LongCount(),
                LatestAtUtc = group.Max(command => command.IssuedAtUtc)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var snapshotAggregate = await dbContext.SignalStateSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.RunId == run.Id)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.LongCount(),
                LatestAtUtc = group.Max(snapshot => snapshot.CapturedAtUtc)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var latestTimestamp = run.FinishedAtUtc
            ?? MaxTimestamp(
                run.StartedAtUtc,
                measurementAggregate?.LatestAtUtc,
                commandAggregate?.LatestAtUtc,
                snapshotAggregate?.LatestAtUtc);

        var durationSeconds = Math.Max(
            0,
            (latestTimestamp - run.StartedAtUtc).TotalSeconds);

        var totalDepartures = measurementAggregate?.TotalDepartures ?? 0;

        return new ExperimentRunMetrics(
            run.Id,
            run.Policy,
            run.Scenario,
            run.StartedAtUtc,
            run.FinishedAtUtc,
            durationSeconds,
            measurementAggregate?.Count ?? 0,
            commandAggregate?.Count ?? 0,
            snapshotAggregate?.Count ?? 0,
            measurementAggregate?.AverageQueueLength ?? 0,
            measurementAggregate?.MaxQueueLength ?? 0,
            measurementAggregate?.TotalArrivals ?? 0,
            totalDepartures,
            totalDepartures);
    }

    private static DateTimeOffset MaxTimestamp(
        DateTimeOffset fallback,
        params DateTimeOffset?[] timestamps)
    {
        var latest = fallback;

        foreach (var timestamp in timestamps)
        {
            if (timestamp is not null && timestamp > latest)
            {
                latest = timestamp.Value;
            }
        }

        return latest;
    }
}
