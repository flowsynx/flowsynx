using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.GeneBlueprints;

public record ExpressionProfile(
    string DefaultOperation,
    List<ExpressionCondition> Conditions,
    int Priority = 1,
    string ExecutionMode = "synchronous");