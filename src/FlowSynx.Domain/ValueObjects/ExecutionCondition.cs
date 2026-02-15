namespace FlowSynx.Domain.ValueObjects;

public record ExecutionCondition(
    string ConditionType,
    string Field,
    string Operator,
    object Value);