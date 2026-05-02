namespace Traffic.Application.Persistence;

public sealed record ExperimentRun(
    Guid Id,
    string Policy,
    string Scenario,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc);
