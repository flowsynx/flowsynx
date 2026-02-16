namespace FlowSynx.Application.Features.WorkflowApplications.Requests.WorkflowApplicationsList;

public class WorkflowApplicationsListResult
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = "default"; 
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
}