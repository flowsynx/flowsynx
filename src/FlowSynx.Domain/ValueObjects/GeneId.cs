namespace FlowSynx.Domain.ValueObjects;

public record ParameterDefinition(
        string Name,
        string Type,
        string Description,
        object DefaultValue,
        bool Required = false,
        List<string> ValidationRules = null);

public record ExpressionCondition(
    string ConditionType,
    string Field,
    string Operator,
    object Value);

public record ExpressionProfile(
    string DefaultOperation,
    List<ExpressionCondition> Conditions,
    int Priority = 1,
    string ExecutionMode = "synchronous");

public record CompatibilityMatrix(
    string MinimumRuntimeVersion,
    List<string> SupportedPlatforms,
    List<string> RequiredDependencies,
    List<string> IncompatibleGenes,
    Dictionary<string, object> RuntimeConstraints);

public record ImmuneResponse(
    string ErrorHandlingPolicy = "propagate",
    int MaxRetries = 3,
    int RetryDelayMs = 100,
    string FallbackOperation = null,
    string HealthCheck = null);

public record ResourceConstraints(
    int MaxMemoryMB = 100,
    int MaxCpuPercent = 50,
    int TimeoutMs = 5000,
    int MaxConcurrent = 5,
    Dictionary<string, object> ResourceLimits = null);

public record ExecutableComponent(
    string Type,
    string Location,
    string EntryPoint,
    string Runtime,
    string Version,
    Dictionary<string, object> Configuration);

public record ExpressionConfiguration(
    string Operation,
    Dictionary<string, object> Parameters,
    List<ExpressionCondition> Conditions);

public record CellularEnvironment(
    ImmuneResponse ErrorHandling,
    ResourceConstraints ResourceConstraints,
    Dictionary<string, object> RuntimeEnvironment,
    Dictionary<string, object> SharedResources);

// Value Objects for Identifiers
public record GeneBlueprintId(string Value)
{
    public static implicit operator string(GeneBlueprintId id) => id.Value;
    public static explicit operator GeneBlueprintId(string value) => new(value);
}

public record GeneInstanceId(string Value)
{
    public static implicit operator string(GeneInstanceId id) => id.Value;
    public static explicit operator GeneInstanceId(string value) => new(value);
}

public record ChromosomeId(string Value)
{
    public static implicit operator string(ChromosomeId id) => id.Value;
    public static explicit operator ChromosomeId(string value) => new(value);
}

public record GenomeId(string Value)
{
    public static implicit operator string(GenomeId id) => id.Value;
    public static explicit operator GenomeId(string value) => new(value);
}