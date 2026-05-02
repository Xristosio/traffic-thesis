namespace Traffic.Contracts.Configuration;

public sealed class SimulationSettings
{
    public const string SectionName = "Simulation";

    public int TickMilliseconds { get; set; }

    public int? RandomSeed { get; set; }

    public string Scenario { get; set; } = string.Empty;

    public int DepartureRatePerTick { get; set; }

    public int? RunDurationSeconds { get; set; }

    public bool StopProducerAfterRun { get; set; }

    public string ExperimentName { get; set; } = string.Empty;
}
