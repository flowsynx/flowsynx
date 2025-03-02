namespace FlowSynx.Core.Features.PluginConfig.Query.List;

public class PluginConfigListResponse
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}