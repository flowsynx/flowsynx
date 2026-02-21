using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.WorkflowExecutions;
using FlowSynx.Domain.Workflows;

namespace FlowSynx.Domain.ActivityInstances;

public class ActivityRun : AuditableEntity<Guid>, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid WorkflowExecutionId { get; set; }
    public Guid ActivityId { get; set; }  // References Activity definition
    public Guid? ActivityInstanceId { get; set; }  // Optional reference to blueprint instance
    public Dictionary<string, object> Params { get; set; } = new();
    public ActivityConfiguration Configuration { get; set; } = new();
    public string Status { get; set; } = "pending";  // pending, running, completed, failed
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? DurationMilliseconds { get; set; }

    public WorkflowExecution? WorkflowExecution { get; set; }
}