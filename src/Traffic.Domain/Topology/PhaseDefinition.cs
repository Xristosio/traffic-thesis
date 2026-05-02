namespace Traffic.Domain.Topology;

public sealed record PhaseDefinition(
    string Id,
    string? Name,
    IReadOnlyList<string> GreenSignalIds);
