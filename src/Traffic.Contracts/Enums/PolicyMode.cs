using System.Text.Json.Serialization;

namespace Traffic.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<PolicyMode>))]
public enum PolicyMode
{
    FixedTime,
    LongestQueueFirst
}
