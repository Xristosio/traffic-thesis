using Traffic.Contracts.Enums;

namespace Traffic.Application.Policies;

public sealed class UnsupportedSignalDecisionCommandScheduler : ISignalDecisionCommandScheduler
{
    public UnsupportedSignalDecisionCommandScheduler(string configuredPolicyMode)
    {
        ConfiguredPolicyMode = configuredPolicyMode;
    }

    public PolicyMode? ActivePolicy => null;

    public string ConfiguredPolicyMode { get; }

    public string Description =>
        $"Policy mode '{ConfiguredPolicyMode}' is not currently implemented. Command publishing is disabled.";

    public IReadOnlyList<ScheduledSignalDecisionCommand> GetDueCommands(DateTimeOffset utcNow)
    {
        return [];
    }
}
