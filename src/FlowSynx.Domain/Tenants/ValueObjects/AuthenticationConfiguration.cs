namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record AuthenticationConfiguration
{
    public AuthenticationMode Mode { get; init; } = AuthenticationMode.None;
    public BasicConfiguration Basic { get; init; } = new();
    public JwtAuthenticationsConfiguration Jwt { get; init; } = new();
}