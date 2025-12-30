namespace FlowSynx.Domain.GeneBlueprints;

public record CompatibilityMatrix(
    string MinimumRuntimeVersion,
    List<string> SupportedPlatforms,
    List<string> RequiredDependencies,
    List<string> IncompatibleGenes,
    Dictionary<string, object> RuntimeConstraints);
