using Microsoft.AspNetCore.Mvc;
using Traffic.Application.SignalStates;
using Traffic.Contracts.Messages;

namespace Traffic.Gateway.Api.Controllers;

[ApiController]
[Route("api/signal-states")]
public sealed class SignalStatesController(ISignalStateStore signalStateStore) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<SignalStateResponse>> GetSignalStates()
    {
        return signalStateStore.GetCurrentSnapshots(DateTimeOffset.UtcNow)
            .Select(ToSignalStateResponse)
            .ToArray();
    }

    [HttpGet("{intersectionId}")]
    public ActionResult<SignalStateResponse> GetSignalStatesByIntersection(string intersectionId)
    {
        return signalStateStore.TryGetCurrentSnapshot(
            intersectionId,
            DateTimeOffset.UtcNow,
            out var snapshot)
            ? Ok(ToSignalStateResponse(snapshot))
            : NotFound();
    }

    private static SignalStateResponse ToSignalStateResponse(SignalStateSnapshot snapshot)
    {
        return new SignalStateResponse(
            snapshot.IntersectionId,
            snapshot.Signals
                .Select(signal => new SignalStateItemResponse(
                    signal.SignalId,
                    signal.State,
                    signal.ElapsedSeconds,
                    signal.QueueLength))
                .ToArray());
    }
}
