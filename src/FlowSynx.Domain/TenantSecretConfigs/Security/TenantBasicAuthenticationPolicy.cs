namespace FlowSynx.Domain.TenantSecretConfigs.Security;

public sealed record TenantBasicAuthenticationPolicy
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
}