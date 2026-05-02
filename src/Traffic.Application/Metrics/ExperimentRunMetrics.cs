namespace Traffic.Application.Metrics;

public sealed record ExperimentRunMetrics(
    Guid RunId,
    string Policy,
    string Scenario,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    double DurationSeconds,
    long MeasurementCount,
    long DecisionCommandCount,
    long StateSnapshotCount,
    double AverageQueueLength,
    int MaxQueueLength,
    long TotalArrivals,
    long TotalDepartures,
    long ServedVehicles);
