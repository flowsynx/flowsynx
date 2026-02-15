namespace FlowSynx.Domain.Activities;

public class ParameterDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string Description { get; set; } = string.Empty;
    public object? Default { get; set; }
    public bool Required { get; set; } = false;
    public object? Schema { get; set; }
    public List<string> Validation { get; set; } = new List<string>();
}