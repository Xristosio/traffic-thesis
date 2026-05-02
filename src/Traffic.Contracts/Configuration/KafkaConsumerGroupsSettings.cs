namespace Traffic.Contracts.Configuration;

public sealed class KafkaConsumerGroupsSettings
{
    public string DecisionEngine { get; set; } = string.Empty;

    public string Gateway { get; set; } = string.Empty;

    public string GatewayMeasurements { get; set; } = string.Empty;

    public string ProducerState { get; set; } = string.Empty;
}
