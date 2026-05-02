namespace Traffic.Contracts.Configuration;

public sealed class KafkaTopicsSettings
{
    public string Measurements { get; set; } = string.Empty;

    public string Commands { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Metrics { get; set; } = string.Empty;
}
