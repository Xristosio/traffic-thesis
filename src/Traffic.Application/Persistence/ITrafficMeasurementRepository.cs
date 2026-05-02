using Traffic.Contracts.Messages;

namespace Traffic.Application.Persistence;

public interface ITrafficMeasurementRepository
{
    Task AddAsync(TrafficMeasurement measurement, CancellationToken cancellationToken);
}
