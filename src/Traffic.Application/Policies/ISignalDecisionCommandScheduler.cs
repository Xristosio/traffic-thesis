using Traffic.Contracts.Messages;

namespace Traffic.Application.Policies;

public interface ISignalDecisionCommandScheduler
{
    bool IsFixedTimeMode { get; }

    string ConfiguredPolicyMode { get; }

    TimeSpan PhaseDuration { get; }

    IReadOnlyList<SignalDecisionCommand> GetDueCommands(DateTimeOffset utcNow);
}
