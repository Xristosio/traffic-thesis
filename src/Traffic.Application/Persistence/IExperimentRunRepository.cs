namespace Traffic.Application.Persistence;

public interface IExperimentRunRepository
{
    Task<ExperimentRun?> GetByIdAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task EnsureStartedAsync(
        Guid runId,
        string policy,
        string scenario,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken);

    Task MarkFinishedAsync(
        Guid runId,
        DateTimeOffset finishedAtUtc,
        CancellationToken cancellationToken);
}
