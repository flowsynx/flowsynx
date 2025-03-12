namespace FlowSynx.Application.Features.PluginConfig.Query.List;

public class PluginConfigListResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}