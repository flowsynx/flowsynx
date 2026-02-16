using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.WorkflowApplications;

namespace FlowSynx.Domain.Workflows;

public class Workflow : AuditableEntity<Guid>, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkflowSpecification Specification { get; set; } = new WorkflowSpecification();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
    public Guid? WorkflowApplicationId { get; set; }
    public ICollection<ActivityInstances.ActivityInstance> Activities { get; set; } = new List<ActivityInstances.ActivityInstance>();
    public WorkflowApplication? WorkflowApplication { get; set; }
}