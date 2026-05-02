using Traffic.Contracts.Configuration;
using Traffic.Domain.Topology;

namespace Traffic.Application.Topology;

public sealed class TopologyBuilder : ITopologyBuilder
{
    public TrafficTopology Build(TopologySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var validationErrors = Validate(settings).ToArray();
        if (validationErrors.Length > 0)
        {
            throw new InvalidOperationException(
                $"Invalid topology configuration: {string.Join("; ", validationErrors)}");
        }

        var intersections = settings.Intersections
            .Select(intersection => new IntersectionDefinition(
                intersection.Id.Trim(),
                NormalizeOptionalText(intersection.Name),
                intersection.Signals
                    .Select(signal => new SignalDefinition(
                        signal.Id.Trim(),
                        NormalizeOptionalText(signal.Name)))
                    .ToArray()))
            .ToArray();

        return new TrafficTopology(intersections);
    }

    private static IEnumerable<string> Validate(TopologySettings settings)
    {
        if (settings.Intersections is null || settings.Intersections.Count == 0)
        {
            yield return "at least one intersection is required";
            yield break;
        }

        foreach (var error in ValidateIntersectionIds(settings.Intersections))
        {
            yield return error;
        }

        for (var index = 0; index < settings.Intersections.Count; index++)
        {
            var intersection = settings.Intersections[index];
            var intersectionLabel = GetIntersectionLabel(intersection, index);

            if (intersection.Signals is null || intersection.Signals.Count < 2)
            {
                yield return $"{intersectionLabel} must have at least two signals";
                continue;
            }

            foreach (var error in ValidateSignalIds(intersection, intersectionLabel))
            {
                yield return error;
            }
        }
    }

    private static IEnumerable<string> ValidateIntersectionIds(IReadOnlyList<IntersectionSettings> intersections)
    {
        for (var index = 0; index < intersections.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(intersections[index].Id))
            {
                yield return $"intersection at index {index} must have a non-empty Id";
            }
        }

        var duplicateIds = intersections
            .Where(intersection => !string.IsNullOrWhiteSpace(intersection.Id))
            .GroupBy(intersection => intersection.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicateId in duplicateIds)
        {
            yield return $"duplicate intersection Id '{duplicateId}'";
        }
    }

    private static IEnumerable<string> ValidateSignalIds(
        IntersectionSettings intersection,
        string intersectionLabel)
    {
        for (var index = 0; index < intersection.Signals.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(intersection.Signals[index].Id))
            {
                yield return $"{intersectionLabel} signal at index {index} must have a non-empty Id";
            }
        }

        var duplicateIds = intersection.Signals
            .Where(signal => !string.IsNullOrWhiteSpace(signal.Id))
            .GroupBy(signal => signal.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicateId in duplicateIds)
        {
            yield return $"{intersectionLabel} has duplicate signal Id '{duplicateId}'";
        }
    }

    private static string GetIntersectionLabel(IntersectionSettings intersection, int index)
    {
        return string.IsNullOrWhiteSpace(intersection.Id)
            ? $"intersection at index {index}"
            : $"intersection '{intersection.Id.Trim()}'";
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
