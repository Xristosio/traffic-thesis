namespace Traffic.Contracts.Configuration;

public sealed class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;

    public KafkaTopicsSettings Topics { get; set; } = new();
}
