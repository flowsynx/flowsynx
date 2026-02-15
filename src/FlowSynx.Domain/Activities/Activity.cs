using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.Activities;

public class Activity : AuditableEntity<Guid>, IAggregateRoot, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivitySpecification Specification { get; set; } = new ActivitySpecification();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
    public string? Owner { get; set; }
    public string Status { get; set; } = "active";
    public bool IsShared { get; set; }
    public Tenant? Tenant { get; set; }
}