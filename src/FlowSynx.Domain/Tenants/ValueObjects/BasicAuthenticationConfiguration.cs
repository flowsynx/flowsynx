namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record BasicAuthenticationConfiguration
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
}