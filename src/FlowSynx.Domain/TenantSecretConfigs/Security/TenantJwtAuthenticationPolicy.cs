namespace FlowSynx.Domain.TenantSecretConfigs.Security;

public sealed record TenantJwtAuthenticationPolicy
{
    public string Name { get; init; } = string.Empty;
    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public bool RequireHttps { get; init; } = false;
    public List<string> RoleClaimNames { get; init; } = new() { "roles", "role", "groups" };
}