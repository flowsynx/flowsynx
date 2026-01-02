namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record BasicConfiguration
{
    public List<BasicAuthenticationConfiguration> Users { get; init; } = new();
}