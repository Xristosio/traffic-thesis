using System.Collections.Concurrent;
using Traffic.Contracts.Messages;

namespace Traffic.Application.Measurements;

public sealed class LatestMeasurementStore : ILatestMeasurementStore
{
    private readonly ConcurrentDictionary<string, TrafficMeasurement> _measurements =
        new(StringComparer.OrdinalIgnoreCase);

    public void Update(TrafficMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        _measurements[GetKey(measurement.IntersectionId, measurement.SignalId)] = measurement;
    }

    public IReadOnlyCollection<TrafficMeasurement> GetAllLatest()
    {
        return _measurements.Values.ToArray();
    }

    public bool TryGetLatestForIntersection(
        string intersectionId,
        out TrafficMeasurement measurement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(intersectionId);

        var latest = _measurements.Values
            .Where(measurement => string.Equals(
                measurement.IntersectionId,
                intersectionId,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(measurement => measurement.MeasuredAtUtc)
            .FirstOrDefault();

        if (latest is null)
        {
            measurement = null!;
            return false;
        }

        measurement = latest;
        return true;
    }

    private static string GetKey(string intersectionId, string signalId)
    {
        return $"{intersectionId}:{signalId}";
    }
}
