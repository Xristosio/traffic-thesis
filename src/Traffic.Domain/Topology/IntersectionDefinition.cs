namespace Traffic.Domain.Topology;

public sealed record IntersectionDefinition(
    string Id,
    string? Name,
    IReadOnlyList<SignalDefinition> Signals);
