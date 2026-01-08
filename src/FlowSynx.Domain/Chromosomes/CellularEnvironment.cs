using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Chromosomes;

public record CellularEnvironment(
    ImmuneSystem ImmuneSystem,                              // ImmuneSystem
    NutrientConstraints NutrientConstraints,                // Resource constraints as nutrients
    Dictionary<string, object> IntracellularConditions,     // Runtime environment inside cell
    Dictionary<string, object> SharedOrganelles);           // SharedResources