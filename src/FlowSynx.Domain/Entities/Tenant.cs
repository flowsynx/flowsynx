using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Entities;

public class Tenant: AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string ConnectionString { get; set; } = string.Empty;
}