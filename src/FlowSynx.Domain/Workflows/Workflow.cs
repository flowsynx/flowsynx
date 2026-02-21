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
    public WorkflowSpecification Specification { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();     // Arbitrary key-value pairs for additional information
    public Dictionary<string, string> Labels { get; set; } = new();       // Key-value pairs for categorization and filtering
    public Dictionary<string, string> Annotations { get; set; } = new();  // Key-value pairs for additional metadata
    public Guid? WorkflowApplicationId { get; set; }

    public ICollection<ActivityInstance> Activities { get; set; } = new List<ActivityInstance>();
    public WorkflowApplication? WorkflowApplication { get; set; }
}