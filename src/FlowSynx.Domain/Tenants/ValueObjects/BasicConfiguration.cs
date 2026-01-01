namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record BasicConfiguration
{
    public bool Enabled { get; init; } = true;
    public List<BasicAuthenticationConfiguration> Users { get; init; } = new();
}