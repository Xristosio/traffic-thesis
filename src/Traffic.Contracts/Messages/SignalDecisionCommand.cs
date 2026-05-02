using System.Text.Json.Serialization;
using Traffic.Contracts.Enums;

namespace Traffic.Contracts.Messages;

public sealed record SignalDecisionCommand(
    [property: JsonPropertyName("messageId")]
    Guid MessageId,
    [property: JsonPropertyName("runId")]
    Guid RunId,
    [property: JsonPropertyName("schemaVersion")]
    int SchemaVersion,
    [property: JsonPropertyName("intersectionId")]
    string IntersectionId,
    [property: JsonPropertyName("selectedSignalId")]
    string SelectedSignalId,
    [property: JsonPropertyName("policy")]
    PolicyMode Policy,
    [property: JsonPropertyName("greenDurationSeconds")]
    int GreenDurationSeconds,
    [property: JsonPropertyName("yellowDurationSeconds")]
    int YellowDurationSeconds,
    [property: JsonPropertyName("issuedAtUtc")]
    DateTimeOffset IssuedAtUtc)
{
    [JsonPropertyName("selectedPhaseId")]
    public string? SelectedPhaseId { get; init; }

    [JsonPropertyName("selectedSignalIds")]
    public IReadOnlyList<string> SelectedSignalIds { get; init; } = [];
}
