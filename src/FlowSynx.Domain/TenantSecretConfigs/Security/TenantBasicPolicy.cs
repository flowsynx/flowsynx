namespace FlowSynx.Domain.TenantSecretConfigs.Security;

public sealed record TenantBasicPolicy
{
    public List<TenantBasicAuthenticationPolicy> Users { get; init; } = new();
}