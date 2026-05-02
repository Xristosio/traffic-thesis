using Traffic.Contracts.Configuration;
using Traffic.Domain.Topology;

namespace Traffic.Application.Topology;

public interface ITopologyBuilder
{
    TrafficTopology Build(TopologySettings settings);
}
