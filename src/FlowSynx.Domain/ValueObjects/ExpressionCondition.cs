namespace FlowSynx.Domain.ValueObjects;

public record ExpressionCondition(
    string ConditionType,
    string Field,
    string Operator,
    object Value);