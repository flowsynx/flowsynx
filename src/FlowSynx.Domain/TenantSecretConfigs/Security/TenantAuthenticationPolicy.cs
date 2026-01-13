namespace FlowSynx.Domain.TenantSecretConfigs.Security;

public sealed record TenantAuthenticationPolicy
{
    public TenantAuthenticationMode Mode { get; init; } = TenantAuthenticationMode.None;
    public TenantBasicPolicy Basic { get; init; } = new();
    public TenantJwtAuthenticationPolicy Jwt { get; init; } = new();
}