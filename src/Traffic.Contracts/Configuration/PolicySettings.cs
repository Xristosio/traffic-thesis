namespace Traffic.Contracts.Configuration;

public sealed class PolicySettings
{
    public const string SectionName = "Policy";

    public string Mode { get; set; } = string.Empty;

    public FixedTimePolicySettings FixedTime { get; set; } = new();

    public LongestQueueFirstPolicySettings LongestQueueFirst { get; set; } = new();
}
