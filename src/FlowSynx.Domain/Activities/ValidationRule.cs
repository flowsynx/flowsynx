namespace FlowSynx.Domain.Activities;

public class ValidationRule
{
    public string Field { get; set; } = string.Empty;
    public string Rule { get; set; } = string.Empty; // "required", "regex", "min", "max", "custom"
    public object Value { get; set; }
    public string Message { get; set; } = string.Empty;
}