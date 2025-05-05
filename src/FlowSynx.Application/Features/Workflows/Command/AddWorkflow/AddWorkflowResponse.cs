namespace FlowSynx.Application.Features.Workflows.Command.AddWorkflow;

public class AddWorkflowResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}