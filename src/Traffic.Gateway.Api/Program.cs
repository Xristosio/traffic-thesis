using Microsoft.Extensions.DependencyInjection;
using Traffic.Gateway.Api;
using Traffic.Gateway.Api.Middleware;
using Traffic.Domain.Topology;
using Traffic.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddTrafficConfiguration(builder.Configuration)
    .AddTrafficTopology()
    .AddTrafficGatewaySignalControl()
    .AddTrafficPersistence();

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHostedService<SignalControllerWorker>();

var app = builder.Build();
LogTopologySummary(app.Services);

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

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
