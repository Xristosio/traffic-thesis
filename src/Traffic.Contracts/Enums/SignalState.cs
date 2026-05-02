using System.Text.Json.Serialization;

namespace Traffic.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<SignalState>))]
public enum SignalState
{
    Red,
    Yellow,
    Green
}
