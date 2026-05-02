using Microsoft.AspNetCore.Mvc;
using Traffic.Domain.Topology;

namespace Traffic.Gateway.Api.Controllers;

[ApiController]
[Route("api/topology")]
public sealed class TopologyController(TrafficTopology topology) : ControllerBase
{
    [HttpGet]
    public ActionResult<TopologyResponse> GetTopology()
    {
        return new TopologyResponse(
            topology.Intersections
                .Select(intersection => new TopologyIntersectionResponse(
                    intersection.Id,
                    intersection.Name,
                    intersection.Signals
                        .Select(signal => new TopologySignalResponse(signal.Id, signal.Name))
                        .ToArray(),
                    intersection.Phases
                        .Select(phase => new TopologyPhaseResponse(
                            phase.Id,
                            phase.Name,
                            phase.GreenSignalIds))
                        .ToArray()))
                .ToArray());
    }
}
