using System.Text.Json.Serialization;

namespace Traffic.Contracts.Messages;

public sealed record MetricSnapshot(
    [property: JsonPropertyName("messageId")]
    Guid MessageId,
    [property: JsonPropertyName("runId")]
    Guid RunId,
    [property: JsonPropertyName("schemaVersion")]
    int SchemaVersion,
    [property: JsonPropertyName("averageQueueLength")]
    double AverageQueueLength,
    [property: JsonPropertyName("maxQueueLength")]
    int MaxQueueLength,
    [property: JsonPropertyName("totalArrivals")]
    int TotalArrivals,
    [property: JsonPropertyName("totalDepartures")]
    int TotalDepartures,
    [property: JsonPropertyName("servedVehicles")]
    int ServedVehicles,
    [property: JsonPropertyName("capturedAtUtc")]
    DateTimeOffset CapturedAtUtc);
