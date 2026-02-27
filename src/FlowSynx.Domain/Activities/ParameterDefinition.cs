namespace FlowSynx.Domain.Activities;

public class ParameterDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string Description { get; set; } = string.Empty;
    public object? Default { get; set; }
    public bool Required { get; set; } = false;
}