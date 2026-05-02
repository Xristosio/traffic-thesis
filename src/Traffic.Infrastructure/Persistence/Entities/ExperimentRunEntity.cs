namespace Traffic.Infrastructure.Persistence.Entities;

public sealed class ExperimentRunEntity
{
    public Guid Id { get; set; }

    public string Policy { get; set; } = string.Empty;

    public string Scenario { get; set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? FinishedAtUtc { get; set; }
}
