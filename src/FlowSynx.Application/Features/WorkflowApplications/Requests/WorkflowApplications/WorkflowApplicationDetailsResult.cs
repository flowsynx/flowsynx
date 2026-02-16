using FlowSynx.Domain.WorkflowApplications;

namespace FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationDetails;

public class WorkflowApplicationDetailsResult
{ 
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowApplicationSpecification Specification { get; set; } = new WorkflowApplicationSpecification();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
}