namespace Traffic.Contracts.Configuration;

public sealed class LongestQueueFirstPolicySettings
{
    public int MinGreenSeconds { get; set; }

    public int MaxGreenSeconds { get; set; }

    public int YellowSeconds { get; set; }

    public int MaxConsecutiveSelections { get; set; }

    public int FairnessQueueThreshold { get; set; }
}
