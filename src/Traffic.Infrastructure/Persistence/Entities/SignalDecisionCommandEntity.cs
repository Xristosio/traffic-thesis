namespace Traffic.Infrastructure.Persistence.Entities;

public sealed class SignalDecisionCommandEntity
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public Guid MessageId { get; set; }

    public string IntersectionId { get; set; } = string.Empty;

    public string SelectedSignalId { get; set; } = string.Empty;

    public string? SelectedPhaseId { get; set; }

    public string SelectedSignalIdsJson { get; set; } = "[]";

    public string Policy { get; set; } = string.Empty;

    public int GreenDurationSeconds { get; set; }

    public int YellowDurationSeconds { get; set; }

    public DateTimeOffset IssuedAtUtc { get; set; }
}
