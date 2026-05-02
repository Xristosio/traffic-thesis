namespace Traffic.Infrastructure.Persistence.Entities;

public sealed class SignalStateItemEntity
{
    public Guid Id { get; set; }

    public Guid SnapshotId { get; set; }

    public string SignalId { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public int ElapsedSeconds { get; set; }

    public int QueueLength { get; set; }

    public SignalStateSnapshotEntity? Snapshot { get; set; }
}
