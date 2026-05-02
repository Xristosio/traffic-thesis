using Microsoft.AspNetCore.Mvc;
using Traffic.Application.Metrics;
using Traffic.Application.Persistence;

namespace Traffic.Gateway.Api.Controllers;

[ApiController]
[Route("api/experiment-runs")]
public sealed class ExperimentRunsController(
    IExperimentMetricsRepository metricsRepository,
    IExperimentRunRepository experimentRunRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExperimentRunListItem>>> GetExperimentRuns(
        CancellationToken cancellationToken)
    {
        var runs = await metricsRepository.GetRunsAsync(cancellationToken);
        return Ok(runs);
    }

    [HttpGet("{runId:guid}/metrics")]
    public async Task<ActionResult<ExperimentRunMetrics>> GetExperimentRunMetrics(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var metrics = await metricsRepository.GetMetricsAsync(runId, cancellationToken);

        return metrics is null
            ? NotFound()
            : Ok(metrics);
    }

    [HttpGet("latest/metrics")]
    public async Task<ActionResult<ExperimentRunMetrics>> GetLatestExperimentRunMetrics(
        CancellationToken cancellationToken)
    {
        var metrics = await metricsRepository.GetLatestMetricsAsync(cancellationToken);

        return metrics is null
            ? NotFound()
            : Ok(metrics);
    }

    [HttpPost("{runId:guid}/finish")]
    public async Task<ActionResult<object>> FinishExperimentRun(
        Guid runId,
        CancellationToken cancellationToken)
    {
        return await MarkRunFinishedAsync(runId, cancellationToken);
    }

    [HttpPost("latest/finish")]
    public async Task<ActionResult<object>> FinishLatestExperimentRun(
        CancellationToken cancellationToken)
    {
        var latestRun = (await metricsRepository.GetRunsAsync(cancellationToken))
            .FirstOrDefault();
        if (latestRun is null)
        {
            return NotFound();
        }

        return await MarkRunFinishedAsync(latestRun.RunId, cancellationToken);
    }

    [HttpGet("compare")]
    public async Task<ActionResult<ExperimentRunComparison>> CompareExperimentRuns(
        [FromQuery] Guid baselineRunId,
        [FromQuery] Guid candidateRunId,
        CancellationToken cancellationToken)
    {
        var comparison = await metricsRepository.CompareRunsAsync(
            baselineRunId,
            candidateRunId,
            cancellationToken);

        return comparison is null
            ? NotFound()
            : Ok(comparison);
    }

    private async Task<ActionResult<object>> MarkRunFinishedAsync(
        Guid runId,
        CancellationToken cancellationToken)
    {
        var existing = await experimentRunRepository.GetByIdAsync(runId, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        await experimentRunRepository.MarkFinishedAsync(
            runId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        var updated = await experimentRunRepository.GetByIdAsync(runId, cancellationToken);
        return Ok(ToExperimentRunResponse(updated ?? existing));
    }

    private static object ToExperimentRunResponse(ExperimentRun run)
    {
        return new
        {
            RunId = run.Id,
            run.Policy,
            run.Scenario,
            run.StartedAtUtc,
            run.FinishedAtUtc
        };
    }
}
