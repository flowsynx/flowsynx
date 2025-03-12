namespace FlowSynx.Application.Features.Workflows.Command.Add;

public class AddWorkflowResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}