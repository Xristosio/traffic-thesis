using Traffic.Contracts.Messages;

namespace Traffic.Application.Measurements;

public interface ILatestMeasurementStore
{
    void Update(TrafficMeasurement measurement);

    IReadOnlyCollection<TrafficMeasurement> GetAllLatest();
}
