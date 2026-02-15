namespace FlowSynx.Domain.ValueObjects;

public record ExecutionProfile(
    string? Operation,
    Dictionary<string, object> Parameters,
    List<ExecutionCondition> Conditions
);