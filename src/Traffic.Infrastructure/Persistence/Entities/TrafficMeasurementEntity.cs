namespace Traffic.Infrastructure.Persistence.Entities;

public sealed class TrafficMeasurementEntity
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public Guid MessageId { get; set; }

    public string IntersectionId { get; set; } = string.Empty;

    public string SignalId { get; set; } = string.Empty;

    public int QueueLength { get; set; }

    public int Arrivals { get; set; }

    public int Departures { get; set; }

    public DateTimeOffset MeasuredAtUtc { get; set; }
}
