namespace FlowSynx.Domain.Primitives;

public interface ITenantScoped
{
    Guid TenantId { get; set; }
}