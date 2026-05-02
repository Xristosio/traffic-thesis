using Microsoft.EntityFrameworkCore;
using Traffic.Application.Persistence;
using Traffic.Contracts.Messages;
using Traffic.Infrastructure.Persistence.Entities;

namespace Traffic.Infrastructure.Persistence.Repositories;

public sealed class TrafficMeasurementRepository(IDbContextFactory<TrafficDbContext> dbContextFactory)
    : ITrafficMeasurementRepository
{
    public async Task AddAsync(
        TrafficMeasurement measurement,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.TrafficMeasurements.Add(new TrafficMeasurementEntity
        {
            Id = Guid.NewGuid(),
            RunId = measurement.RunId,
            MessageId = measurement.MessageId,
            IntersectionId = measurement.IntersectionId,
            SignalId = measurement.SignalId,
            QueueLength = measurement.QueueLength,
            Arrivals = measurement.Arrivals,
            Departures = measurement.Departures,
            MeasuredAtUtc = measurement.MeasuredAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
