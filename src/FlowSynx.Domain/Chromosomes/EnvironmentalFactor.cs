using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Chromosomes;

public record EnvironmentalFactor(
    DefenseMechanism DefenseMechanism,
    ResourceConstraints ResourceConstraints,
    Dictionary<string, object> RuntimeEnvironment,
    Dictionary<string, object> SharedResources);