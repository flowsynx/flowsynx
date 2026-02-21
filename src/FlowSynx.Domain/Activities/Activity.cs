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
    public string? Description { get; set; }  // Short summary
    public ActivitySpecification Specification { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();    // Arbitrary key-value pairs for additional information
    public Dictionary<string, string> Labels { get; set; } = new();      // Key-value pairs for categorization and filtering
    public Dictionary<string, string> Annotations { get; set; } = new(); // Key-value pairs for additional metadata
    public string? Owner { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Active;
    public bool IsShared { get; set; }
    public Tenant? Tenant { get; set; }
}