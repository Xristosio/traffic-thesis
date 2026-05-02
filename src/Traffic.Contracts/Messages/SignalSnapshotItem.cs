using System.Text.Json.Serialization;
using Traffic.Contracts.Enums;

namespace Traffic.Contracts.Messages;

public sealed record SignalSnapshotItem(
    [property: JsonPropertyName("signalId")]
    string SignalId,
    [property: JsonPropertyName("state")]
    SignalState State,
    [property: JsonPropertyName("elapsedSeconds")]
    int ElapsedSeconds,
    [property: JsonPropertyName("queueLength")]
    int QueueLength);
