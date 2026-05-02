using Traffic.Contracts.Messages;

namespace Traffic.Application.Measurements;

public interface ILatestMeasurementStore
{
    void Update(TrafficMeasurement measurement);

    IReadOnlyCollection<TrafficMeasurement> GetAllLatest();

    IReadOnlyCollection<TrafficMeasurement> GetLatestForIntersection(string intersectionId);

    bool TryGetLatestForIntersection(
        string intersectionId,
        out TrafficMeasurement measurement);
}
