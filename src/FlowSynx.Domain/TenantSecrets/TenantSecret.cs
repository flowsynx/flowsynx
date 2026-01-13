using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.TenantSecrets;

public class TenantSecret : AuditableEntity<Guid>, IAggregateRoot
{
    private TenantSecret() { } // EF Core constructor

    private TenantSecret(TenantId tenantId, SecretKey key, SecretValue value)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Key = key;
        Value = value;
    }

    public static TenantSecret Create(TenantId tenantId, SecretKey key, SecretValue value)
    {
        return new TenantSecret(tenantId, key, value);
    }

    public TenantId TenantId { get; private set; }
    public SecretKey Key { get; private set; }
    public SecretValue Value { get; private set; }
    public Tenant? Tenant { get; set; }

    public void UpdateValue(SecretValue newValue)
    {
        if (Value == newValue) return;
        Value = newValue;
    }

    public bool IsExpired => Value.ExpiresAt.HasValue && Value.ExpiresAt.Value < DateTime.UtcNow;
}
