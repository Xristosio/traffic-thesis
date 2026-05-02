using Traffic.Contracts.Messages;
using Traffic.Contracts.Enums;

namespace Traffic.Application.Policies;

public interface ISignalDecisionCommandScheduler
{
    PolicyMode? ActivePolicy { get; }

    string ConfiguredPolicyMode { get; }

    string Description { get; }

    IReadOnlyList<ScheduledSignalDecisionCommand> GetDueCommands(DateTimeOffset utcNow);
}

public sealed record ScheduledSignalDecisionCommand(
    SignalDecisionCommand Command,
    string Reason,
    int SelectedQueueLength,
    string? PreviousSignalId = null,
    bool IsFairnessSwitch = false,
    string? PreviousPhaseId = null);
