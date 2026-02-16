namespace FlowSynx.Application.Features.WorkflowApplications.Actions.CreateWorkflowApplication;

public class CreateWorkflowApplicationResult
{
    public string? Status { get; set; }
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Namespace { get; set; }
}