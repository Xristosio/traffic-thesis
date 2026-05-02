namespace Traffic.Application.Simulation;

public interface IProducerSimulationService
{
    Guid RunId { get; }

    IReadOnlyList<GeneratedTrafficMeasurement> GenerateMeasurements();
}
