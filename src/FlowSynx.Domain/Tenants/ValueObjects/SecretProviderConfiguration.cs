namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record InfisicalSecretConfiguration
{
    public bool Enabled { get; init; }
    public string? HostUri { get; init; }
    public string ProjectId { get; init; } = string.Empty;
    public string EnvironmentSlug { get; init; } = string.Empty;
    public string SecretPath { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    public static InfisicalSecretConfiguration Create()
    {
        return new InfisicalSecretConfiguration
        {
            Enabled = false,
            HostUri = null,
            ProjectId = string.Empty,
            EnvironmentSlug = string.Empty,
            SecretPath = string.Empty,
            ClientId = string.Empty,
            ClientSecret = string.Empty
        };
    }
}