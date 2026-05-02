using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Traffic.Infrastructure.Persistence;

public sealed class TrafficDbContextFactory : IDesignTimeDbContextFactory<TrafficDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5433;Database=traffic_thesis;Username=postgres;Password=postgres";

    public TrafficDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TRAFFIC_DATABASE_CONNECTION_STRING")
            ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<TrafficDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TrafficDbContext(options);
    }
}
