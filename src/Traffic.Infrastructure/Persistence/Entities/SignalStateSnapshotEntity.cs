namespace Traffic.Infrastructure.Persistence.Entities;

public sealed class SignalStateSnapshotEntity
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public Guid MessageId { get; set; }

    public string IntersectionId { get; set; } = string.Empty;

    public DateTimeOffset CapturedAtUtc { get; set; }

    public List<SignalStateItemEntity> Signals { get; set; } = [];
}
