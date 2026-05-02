namespace Traffic.Contracts.Configuration;

public sealed class PhaseSettings
{
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }

    public List<string> GreenSignalIds { get; set; } = [];
}
