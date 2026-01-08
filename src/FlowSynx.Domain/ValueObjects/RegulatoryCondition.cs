namespace FlowSynx.Domain.ValueObjects;

public record RegulatoryCondition(
    string ConditionType,
    string Field,
    string Operator,
    object Value);