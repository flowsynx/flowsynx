namespace FlowSynx.Domain.Plugin;

public class PluginOperation
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<PluginOperationParameter> Parameters { get; set; } = new List<PluginOperationParameter>();
}