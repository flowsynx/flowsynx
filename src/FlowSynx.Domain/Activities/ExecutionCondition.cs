namespace FlowSynx.Domain.Activities;

public class ExecutionCondition
{
    public string When { get; set; } = "always"; // "always", "onSuccess", "onFailure", "custom"
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "equals";
    public object? Value { get; set; }
    public string Action { get; set; } = "skip"; // "skip", "execute", "fail"
}