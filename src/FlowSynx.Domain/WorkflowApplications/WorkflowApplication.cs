using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.WorkflowExecutions;
using FlowSynx.Domain.Workflows;

namespace FlowSynx.Domain.WorkflowApplications;

public class WorkflowApplication : AuditableEntity<Guid>, IAggregateRoot, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public WorkflowApplicationSpecification Specification { get; set; } = new();

    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, string> Labels { get; set; } = new();
    public Dictionary<string, string> Annotations { get; set; } = new();

    public Dictionary<string, object> SharedContext { get; set; } = new();

    public string Owner { get; set; } = string.Empty;
    public bool IsShared { get; set; }

    public ICollection<Workflow> Workflows { get; set; } = new List<Workflow>();
    public ICollection<WorkflowExecution> Executions { get; set; } = new List<WorkflowExecution>();
}