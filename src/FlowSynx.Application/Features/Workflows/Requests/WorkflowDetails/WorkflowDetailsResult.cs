using FlowSynx.Domain.Workflows;

namespace FlowSynx.Application.Features.Workflows.Requests.WorkflowDetails;

public class WorkflowDetailsResult
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string Description { get; set; }
    public WorkflowSpecification Specification { get; set; } = new WorkflowSpecification();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
}