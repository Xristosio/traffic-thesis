using Traffic.Contracts.Enums;
using Traffic.Contracts.Messages;

namespace Traffic.Application.Simulation;

public sealed record GeneratedTrafficMeasurement(
    TrafficMeasurement Measurement,
    SignalState SignalState);
