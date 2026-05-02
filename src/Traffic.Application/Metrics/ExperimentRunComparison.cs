namespace Traffic.Application.Metrics;

public sealed record ExperimentRunComparison(
    Guid BaselineRunId,
    Guid CandidateRunId,
    string BaselinePolicy,
    string CandidatePolicy,
    string BaselineScenario,
    string CandidateScenario,
    ExperimentRunComparisonMetrics BaselineMetrics,
    ExperimentRunComparisonMetrics CandidateMetrics,
    double AverageQueueLengthDelta,
    int MaxQueueLengthDelta,
    long TotalDeparturesDelta,
    long ServedVehiclesDelta,
    double? AverageQueueLengthImprovementPercent,
    double? MaxQueueLengthImprovementPercent,
    double? ServedVehiclesImprovementPercent);

public sealed record ExperimentRunComparisonMetrics(
    double AverageQueueLength,
    int MaxQueueLength,
    long TotalArrivals,
    long TotalDepartures,
    long ServedVehicles,
    double DurationSeconds);
