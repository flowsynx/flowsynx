namespace FlowSynx.Core.Features.Config.Command.Add;

public class AddConfigResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}