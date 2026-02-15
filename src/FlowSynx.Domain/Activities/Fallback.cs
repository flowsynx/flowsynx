namespace FlowSynx.Domain.Activities;

public class Fallback
{
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public object? DefaultValue { get; set; }
}