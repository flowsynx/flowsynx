namespace FlowSynx.Application.Features.Plugins.Query.Details;

public class PluginDetailsSpecification
{
    public required string Key { get; set; }
    public required string Type { get; set; }
    public bool IsRequired { get; set; } = false;
}