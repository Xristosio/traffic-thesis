namespace Traffic.Contracts.Configuration;

public sealed class FixedTimePolicySettings
{
    public int GreenSeconds { get; set; }

    public int YellowSeconds { get; set; }
}
