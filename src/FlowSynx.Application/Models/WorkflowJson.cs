using FlowSynx.Domain.Workflows;

namespace FlowSynx.Application.Models;

public class WorkflowJson
{
    public string ApiVersion { get; set; } = "workflow/v1";
    public string Kind { get; set; } = "Workflow";
    public WorkflowMetadata Metadata { get; set; } = new WorkflowMetadata();
    public WorkflowSpecification Spec { get; set; } = new WorkflowSpecification();
}