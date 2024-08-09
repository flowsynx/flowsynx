namespace FlowSynx.Core.Features.Config.Query.List;

public class ConfigListResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}