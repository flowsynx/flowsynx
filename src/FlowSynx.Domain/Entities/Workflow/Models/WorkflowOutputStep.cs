namespace FlowSynx.Domain.Entities.Workflow.Models;

public class WorkflowOutputStep
{
    public string? Description { get; set; }
    public required string Value { get; set; }
}