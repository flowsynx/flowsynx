using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Events;

namespace FlowSynx.Domain.TenantSecretConfigs;

public class TenantSecretConfig : AuditableEntity<Guid>, IAggregateRoot
{
    private TenantSecretConfig() { } // EF Core constructor

    private TenantSecretConfig(TenantId tenantId, SecretProviderType providerType, ProviderConfiguration configuration)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        ProviderType = providerType;
        Configuration = configuration;
        IsEnabled = true;
        CacheDurationMinutes = 5;
    }

    public static TenantSecretConfig Create(TenantId tenantId, SecretProviderType providerType,
        ProviderConfiguration configuration)
    {
        return new TenantSecretConfig(tenantId, providerType, configuration);
    }

    public TenantId TenantId { get; private set; }
    public SecretProviderType ProviderType { get; private set; }
    public ProviderConfiguration Configuration { get; private set; }
    public bool IsEnabled { get; private set; }
    public int CacheDurationMinutes { get; private set; }
    public Tenant? Tenant { get; set; }

    public void UpdateConfiguration(SecretProviderType providerType, ProviderConfiguration configuration)
    {
        ProviderType = providerType;
        Configuration = configuration;

        AddDomainEvent(new SecretConfigUpdatedEvent(TenantId.Value, Id));
    }

    public void Enable()
    {
        if (IsEnabled) return;

        IsEnabled = true;
        AddDomainEvent(new SecretConfigEnabledEvent(TenantId.Value, Id));
    }

    public void Disable()
    {
        if (!IsEnabled) return;

        IsEnabled = false;
        AddDomainEvent(new SecretConfigDisabledEvent(TenantId.Value, Id));
    }

    public void UpdateCacheDuration(int minutes)
    {
        if (minutes < 1 || minutes > 1440) // 24 hours max
            throw new TenantCacheDurationOutOfRangeException();

        CacheDurationMinutes = minutes;
    }
}
