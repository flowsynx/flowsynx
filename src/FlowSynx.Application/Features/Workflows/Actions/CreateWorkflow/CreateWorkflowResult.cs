namespace FlowSynx.Application.Features.Workflows.Actions.CreateWorkflow;

public class CreateWorkflowResult
{
    public string? Status { get; set; }
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Namespace { get; set; }
}