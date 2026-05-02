using Microsoft.Extensions.DependencyInjection;
using Traffic.Domain.Topology;
using Traffic.Infrastructure.DependencyInjection;
using Traffic.Producer.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddTrafficConfiguration(builder.Configuration)
    .AddTrafficTopology();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
LogTopologySummary(host.Services);
host.Run();

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
