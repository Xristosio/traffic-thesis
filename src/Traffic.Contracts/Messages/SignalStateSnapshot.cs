using System.Text.Json.Serialization;

namespace Traffic.Contracts.Messages;

public sealed record SignalStateSnapshot(
    [property: JsonPropertyName("messageId")]
    Guid MessageId,
    [property: JsonPropertyName("runId")]
    Guid RunId,
    [property: JsonPropertyName("schemaVersion")]
    int SchemaVersion,
    [property: JsonPropertyName("intersectionId")]
    string IntersectionId,
    [property: JsonPropertyName("signals")]
    IReadOnlyList<SignalSnapshotItem> Signals,
    [property: JsonPropertyName("capturedAtUtc")]
    DateTimeOffset CapturedAtUtc);
