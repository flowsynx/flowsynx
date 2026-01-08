namespace FlowSynx.Domain.ValueObjects;

public record ExpressionConfiguration(
    string Operation,
    Dictionary<string, object> Parameters,
    List<ExpressionCondition> Conditions);