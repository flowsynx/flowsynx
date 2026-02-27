namespace FlowSynx.Domain.Activities;

public class ParameterDefinition
{
    public string Name { get; set; } = string.Empty;
    public ParameterType Type { get; set; } = ParameterType.String;
    public string Description { get; set; } = string.Empty;
    public object? Default { get; set; }
    public bool Required { get; set; } = false;
}