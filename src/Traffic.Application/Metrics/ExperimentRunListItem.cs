namespace Traffic.Application.Metrics;

public sealed record ExperimentRunListItem(
    Guid RunId,
    string Policy,
    string Scenario,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc);
