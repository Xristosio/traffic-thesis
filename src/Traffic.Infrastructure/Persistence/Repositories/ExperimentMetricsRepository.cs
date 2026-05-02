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

    public async Task<ExperimentRunComparison?> CompareRunsAsync(
        Guid baselineRunId,
        Guid candidateRunId,
        CancellationToken cancellationToken)
    {
        var baseline = await GetMetricsAsync(baselineRunId, cancellationToken);
        if (baseline is null)
        {
            return null;
        }

        var candidate = await GetMetricsAsync(candidateRunId, cancellationToken);
        if (candidate is null)
        {
            return null;
        }

        return BuildComparison(baseline, candidate);
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

    private static ExperimentRunComparison BuildComparison(
        ExperimentRunMetrics baseline,
        ExperimentRunMetrics candidate)
    {
        return new ExperimentRunComparison(
            baseline.RunId,
            candidate.RunId,
            baseline.Policy,
            candidate.Policy,
            baseline.Scenario,
            candidate.Scenario,
            ToComparisonMetrics(baseline),
            ToComparisonMetrics(candidate),
            candidate.AverageQueueLength - baseline.AverageQueueLength,
            candidate.MaxQueueLength - baseline.MaxQueueLength,
            candidate.TotalDepartures - baseline.TotalDepartures,
            candidate.ServedVehicles - baseline.ServedVehicles,
            CalculateLowerIsBetterImprovement(
                baseline.AverageQueueLength,
                candidate.AverageQueueLength),
            CalculateLowerIsBetterImprovement(
                baseline.MaxQueueLength,
                candidate.MaxQueueLength),
            CalculateHigherIsBetterImprovement(
                baseline.ServedVehicles,
                candidate.ServedVehicles));
    }

    private static ExperimentRunComparisonMetrics ToComparisonMetrics(ExperimentRunMetrics metrics)
    {
        return new ExperimentRunComparisonMetrics(
            metrics.AverageQueueLength,
            metrics.MaxQueueLength,
            metrics.TotalArrivals,
            metrics.TotalDepartures,
            metrics.ServedVehicles,
            metrics.DurationSeconds);
    }

    private static double? CalculateLowerIsBetterImprovement(
        double baseline,
        double candidate)
    {
        return baseline > 0
            ? ((baseline - candidate) / baseline) * 100
            : null;
    }

    private static double? CalculateHigherIsBetterImprovement(
        long baseline,
        long candidate)
    {
        return baseline > 0
            ? ((candidate - baseline) / (double)baseline) * 100
            : null;
    }
}
