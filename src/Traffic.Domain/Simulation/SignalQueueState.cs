namespace Traffic.Domain.Simulation;

public sealed class SignalQueueState
{
    public SignalQueueState(string intersectionId, string signalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(intersectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(signalId);

        IntersectionId = intersectionId;
        SignalId = signalId;
    }

    public string IntersectionId { get; }

    public string SignalId { get; }

    public int CurrentQueueLength { get; private set; }

    public void ApplyMeasurementDelta(int arrivals, int departures)
    {
        if (arrivals < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrivals), "Arrivals cannot be negative.");
        }

        if (departures < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departures), "Departures cannot be negative.");
        }

        CurrentQueueLength = Math.Max(0, CurrentQueueLength + arrivals - departures);
    }
}
