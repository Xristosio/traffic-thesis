using Traffic.Contracts.Messages;

namespace Traffic.Application.Simulation;

public interface IProducerSimulationService
{
    Guid RunId { get; }

    IReadOnlyList<TrafficMeasurement> GenerateMeasurements();
}
