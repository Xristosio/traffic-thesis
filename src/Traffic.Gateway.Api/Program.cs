using Microsoft.Extensions.DependencyInjection;
using Traffic.Gateway.Api;
using Traffic.Domain.Topology;
using Traffic.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddTrafficConfiguration(builder.Configuration)
    .AddTrafficTopology()
    .AddTrafficGatewaySignalControl();

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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
