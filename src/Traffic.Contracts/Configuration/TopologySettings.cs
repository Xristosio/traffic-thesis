namespace Traffic.Contracts.Configuration;

public sealed class TopologySettings
{
    public const string SectionName = "Topology";

    public List<IntersectionSettings> Intersections { get; set; } = [];
}
