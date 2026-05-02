namespace Traffic.Contracts.Configuration;

public sealed class IntersectionSettings
{
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }

    public List<SignalSettings> Signals { get; set; } = [];

    public List<PhaseSettings> Phases { get; set; } = [];
}
