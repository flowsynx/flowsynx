using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Workflows;

namespace FlowSynx.Domain.ActivityInstances;

public class ActivityInstance : AuditableEntity<Guid>, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public ActivityConfiguration Configuration { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Guid WorkflowId { get; set; }
    public int Order { get; set; }
    public Workflow? Workflow { get; set; }
}