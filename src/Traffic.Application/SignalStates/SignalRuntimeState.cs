using Traffic.Contracts.Enums;

namespace Traffic.Application.SignalStates;

public sealed class SignalRuntimeState
{
    public SignalRuntimeState(string intersectionId, string signalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(intersectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(signalId);

        IntersectionId = intersectionId;
        SignalId = signalId;
        CurrentState = SignalState.Red;
    }

    public string IntersectionId { get; }

    public string SignalId { get; }

    public SignalState CurrentState { get; private set; }

    public int ElapsedSeconds { get; private set; }

    public int QueueLength { get; private set; }

    public void Update(SignalState currentState, int elapsedSeconds, int queueLength)
    {
        if (elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed seconds cannot be negative.");
        }

        if (queueLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(queueLength), "Queue length cannot be negative.");
        }

        CurrentState = currentState;
        ElapsedSeconds = elapsedSeconds;
        QueueLength = queueLength;
    }
}
