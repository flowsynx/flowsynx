namespace FlowSynx.Application.Features.Workflows.Command.OptimizeWorkflow;

public class OptimizeWorkflowResponse
{
    public required string OptimizedWorkflowJson { get; init; }
    public required string Explanation { get; init; }
    public Guid? WorkflowId { get; init; }
}