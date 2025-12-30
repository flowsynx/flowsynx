using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Entities;

public class AuditTrail : Entity<long>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public string? PrimaryKey { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedColumns { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}