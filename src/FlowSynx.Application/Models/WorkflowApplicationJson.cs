using FlowSynx.Domain.WorkflowApplications;

namespace FlowSynx.Application.Models;

public class WorkflowApplicationJson
{
    public string ApiVersion { get; set; } = "application/v1";
    public string Kind { get; set; } = "Application";
    public WorkflowApplicationMetadata Metadata { get; set; } = new WorkflowApplicationMetadata();
    public WorkflowApplicationSpecification Specification { get; set; } = new WorkflowApplicationSpecification();
}