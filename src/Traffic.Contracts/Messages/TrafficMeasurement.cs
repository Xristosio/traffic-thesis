using System.Text.Json.Serialization;

namespace Traffic.Contracts.Messages;

public sealed record TrafficMeasurement(
    [property: JsonPropertyName("messageId")]
    Guid MessageId,
    [property: JsonPropertyName("runId")]
    Guid RunId,
    [property: JsonPropertyName("schemaVersion")]
    int SchemaVersion,
    [property: JsonPropertyName("intersectionId")]
    string IntersectionId,
    [property: JsonPropertyName("signalId")]
    string SignalId,
    [property: JsonPropertyName("queueLength")]
    int QueueLength,
    [property: JsonPropertyName("arrivals")]
    int Arrivals,
    [property: JsonPropertyName("departures")]
    int Departures,
    [property: JsonPropertyName("measuredAtUtc")]
    DateTimeOffset MeasuredAtUtc);
