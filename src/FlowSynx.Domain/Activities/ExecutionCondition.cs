namespace FlowSynx.Domain.Activities;

public class ExecutionCondition
{
    public ExecutionConditionWhen When { get; set; } = ExecutionConditionWhen.Always;
    public string Field { get; set; } = string.Empty;
    public ExecutionConditionOperator Operator { get; set; } = ExecutionConditionOperator.Equals;
    public object? Value { get; set; }
    public ExecutionConditionAction Action { get; set; } = ExecutionConditionAction.Skip;
}