using Microsoft.Extensions.DependencyInjection;
using Traffic.Gateway.Api;
using Traffic.Application.Metrics;
using Traffic.Application.Persistence;
using Traffic.Application.SignalStates;
using Traffic.Domain.Topology;
using Traffic.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddTrafficConfiguration(builder.Configuration)
    .AddTrafficTopology()
    .AddTrafficGatewaySignalControl()
    .AddTrafficPersistence();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHostedService<SignalControllerWorker>();

var app = builder.Build();
LogTopologySummary(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/topology", (TrafficTopology topology) =>
    new TopologyResponse(
        topology.Intersections
            .Select(intersection => new TopologyIntersectionResponse(
                intersection.Id,
                intersection.Name,
                intersection.Signals
                    .Select(signal => new TopologySignalResponse(signal.Id, signal.Name))
                    .ToArray(),
                intersection.Phases
                    .Select(phase => new TopologyPhaseResponse(
                        phase.Id,
                        phase.Name,
                        phase.GreenSignalIds))
                    .ToArray()))
            .ToArray()))
    .WithName("GetTopology");

app.MapGet("/api/signal-states", (ISignalStateStore signalStateStore) =>
    signalStateStore.GetCurrentSnapshots(DateTimeOffset.UtcNow)
        .Select(ToSignalStateResponse)
        .ToArray())
    .WithName("GetSignalStates");

app.MapGet(
    "/api/signal-states/{intersectionId}",
    (string intersectionId, ISignalStateStore signalStateStore) =>
    {
        return signalStateStore.TryGetCurrentSnapshot(
            intersectionId,
            DateTimeOffset.UtcNow,
            out var snapshot)
            ? Results.Ok(ToSignalStateResponse(snapshot))
            : Results.NotFound();
    })
    .WithName("GetSignalStatesByIntersection");

app.MapGet(
    "/api/experiment-runs",
    async (IExperimentMetricsRepository metricsRepository, CancellationToken cancellationToken) =>
        await metricsRepository.GetRunsAsync(cancellationToken))
    .WithName("GetExperimentRuns");

app.MapGet(
    "/api/experiment-runs/latest/metrics",
    async (IExperimentMetricsRepository metricsRepository, CancellationToken cancellationToken) =>
    {
        var metrics = await metricsRepository.GetLatestMetricsAsync(cancellationToken);

        return metrics is null
            ? Results.NotFound()
            : Results.Ok(metrics);
    })
    .WithName("GetLatestExperimentRunMetrics");

app.MapGet(
    "/api/experiment-runs/compare",
    async (
        Guid baselineRunId,
        Guid candidateRunId,
        IExperimentMetricsRepository metricsRepository,
        CancellationToken cancellationToken) =>
    {
        var comparison = await metricsRepository.CompareRunsAsync(
            baselineRunId,
            candidateRunId,
            cancellationToken);

        return comparison is null
            ? Results.NotFound()
            : Results.Ok(comparison);
    })
    .WithName("CompareExperimentRuns");

app.MapGet(
    "/api/experiment-runs/{runId:guid}/metrics",
    async (Guid runId, IExperimentMetricsRepository metricsRepository, CancellationToken cancellationToken) =>
    {
        var metrics = await metricsRepository.GetMetricsAsync(runId, cancellationToken);

        return metrics is null
            ? Results.NotFound()
            : Results.Ok(metrics);
    })
    .WithName("GetExperimentRunMetrics");

app.MapPost(
    "/api/experiment-runs/latest/finish",
    async (
        IExperimentMetricsRepository metricsRepository,
        IExperimentRunRepository experimentRunRepository,
        CancellationToken cancellationToken) =>
    {
        var latestRun = (await metricsRepository.GetRunsAsync(cancellationToken))
            .FirstOrDefault();
        if (latestRun is null)
        {
            return Results.NotFound();
        }

        return await MarkRunFinishedAsync(
            latestRun.RunId,
            experimentRunRepository,
            cancellationToken);
    })
    .WithName("FinishLatestExperimentRun");

app.MapPost(
    "/api/experiment-runs/{runId:guid}/finish",
    async (
        Guid runId,
        IExperimentRunRepository experimentRunRepository,
        CancellationToken cancellationToken) =>
        await MarkRunFinishedAsync(runId, experimentRunRepository, cancellationToken))
    .WithName("FinishExperimentRun");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

static void LogTopologySummary(IServiceProvider serviceProvider)
{
    var topology = serviceProvider.GetRequiredService<TrafficTopology>();
    var logger = serviceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("TrafficTopology");

    logger.LogInformation(
        "Loaded topology: {IntersectionCount} intersections, {TotalSignals} total signals.",
        topology.Intersections.Count,
        topology.TotalSignals);

    foreach (var intersection in topology.Intersections)
    {
        logger.LogInformation(
            "{IntersectionId}: {SignalCount} signals",
            intersection.Id,
            intersection.Signals.Count);
    }
}

static SignalStateResponse ToSignalStateResponse(Traffic.Contracts.Messages.SignalStateSnapshot snapshot)
{
    return new SignalStateResponse(
        snapshot.IntersectionId,
        snapshot.Signals
            .Select(signal => new SignalStateItemResponse(
                signal.SignalId,
                signal.State,
                signal.ElapsedSeconds,
                signal.QueueLength))
            .ToArray());
}

static async Task<IResult> MarkRunFinishedAsync(
    Guid runId,
    IExperimentRunRepository experimentRunRepository,
    CancellationToken cancellationToken)
{
    var existing = await experimentRunRepository.GetByIdAsync(runId, cancellationToken);
    if (existing is null)
    {
        return Results.NotFound();
    }

    await experimentRunRepository.MarkFinishedAsync(
        runId,
        DateTimeOffset.UtcNow,
        cancellationToken);

    var updated = await experimentRunRepository.GetByIdAsync(runId, cancellationToken);
    return Results.Ok(ToExperimentRunResponse(updated ?? existing));
}

static object ToExperimentRunResponse(ExperimentRun run)
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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
