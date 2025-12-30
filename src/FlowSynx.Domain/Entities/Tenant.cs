using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Entities;

public class Tenant: AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Timezone { get; set; } = "UTC";
    public string Locale { get; set; } = "en-US";
    public bool IsActive { get; set; } = true;
    public int MaxUsers { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
}