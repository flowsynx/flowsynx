namespace FlowSynx.Domain.ValueObjects;

public record ExpressionProfile(
    string? ExpressedBehavior,                           // Operation
    Dictionary<string, object> Nucleotides,              // Parameters
    List<RegulatoryCondition> RegulatoryConditions       // Conditions
);              