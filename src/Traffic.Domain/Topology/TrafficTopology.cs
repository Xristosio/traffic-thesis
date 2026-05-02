namespace Traffic.Domain.Topology;

public sealed record TrafficTopology(IReadOnlyList<IntersectionDefinition> Intersections)
{
    public int TotalSignals => Intersections.Sum(intersection => intersection.Signals.Count);
}
