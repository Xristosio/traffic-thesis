using Traffic.Contracts.Enums;

namespace Traffic.Gateway.Api;

public sealed record TopologyResponse(IReadOnlyList<TopologyIntersectionResponse> Intersections);

public sealed record TopologyIntersectionResponse(
    string IntersectionId,
    string? Name,
    IReadOnlyList<TopologySignalResponse> Signals,
    IReadOnlyList<TopologyPhaseResponse> Phases);

public sealed record TopologySignalResponse(string SignalId, string? Name);

public sealed record TopologyPhaseResponse(
    string PhaseId,
    string? Name,
    IReadOnlyList<string> GreenSignalIds);

public sealed record SignalStateResponse(
    string IntersectionId,
    IReadOnlyList<SignalStateItemResponse> Signals);

public sealed record SignalStateItemResponse(
    string SignalId,
    SignalState State,
    int ElapsedSeconds,
    int QueueLength);
