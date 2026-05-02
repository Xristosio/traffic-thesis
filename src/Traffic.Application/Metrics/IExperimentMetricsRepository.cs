namespace Traffic.Application.Metrics;

public interface IExperimentMetricsRepository
{
    Task<IReadOnlyList<ExperimentRunListItem>> GetRunsAsync(CancellationToken cancellationToken);

    Task<ExperimentRunMetrics?> GetMetricsAsync(
        Guid runId,
        CancellationToken cancellationToken);

    Task<ExperimentRunMetrics?> GetLatestMetricsAsync(CancellationToken cancellationToken);

    Task<ExperimentRunComparison?> CompareRunsAsync(
        Guid baselineRunId,
        Guid candidateRunId,
        CancellationToken cancellationToken);
}
